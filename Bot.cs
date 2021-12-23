using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace RankBot
{
    class Bot
    {
        public static Bot Instance;
        // public DiscordWrapper dwrap;
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private bool roleInitComplete = false;
        private bool constructionComplete = false;
        private bool _primaryServerLoaded = false;
        private string botStatus = Settings.botStatus;

        public BotDataStructure _data;

        // The primary guild, used for backing up data as well as loading configuration data about other guilds.
        private PrimaryDiscordGuild _primary;
        // The other guilds (possibly including the primary one) where tracking and interfacing with users take place.
        // private List<DiscordGuild> _guildList;
        private DiscordGuilds _guilds;

        // Parameters used by extensions, might be null if extensions are turned off.
        private Extensions.MainHighlighter _highlighter;
        private Extensions.BanTracking _bt;

        public Bot()
        {
            Instance = this;
        }

        /// <summary>
        /// An initial construction, if the bot is run for the first time.
        /// </summary>
        /// <returns></returns>
        public void FirstRunDelayedConstruction()
        {
            if (File.Exists(Settings.backupFile))
            {
                Console.WriteLine("Warning: There already exists a backup file, but your first run variable is set to true. Please keep it in mind");
            }

            _data = new BotDataStructure();

            if (Settings.UsingExtensionBanTracking)
            {
                _bt = new Extensions.BanTracking(_guilds);
            }

            if (Settings.UsingExtensionRoleHighlights)
            {
                _highlighter = new Extensions.MainHighlighter(_guilds);
            }
        }

        /// <summary>
        /// Fills in the data fields from some backup. Previously we did that on Bot() initialization,
        /// we do that in a separate function now.
        /// </summary>
        /// <returns></returns>
        public async Task DelayedConstruction()
        {
            if (_primary == null)
            {
                throw new PrimaryGuildException("DelayedConstruction() called too early, the Discord API is not ready yet.");
            }

            if (Settings.BotFirstRun)
            {
                FirstRunDelayedConstruction();
                return;
            }

            if (constructionComplete)
            {
                throw new PrimaryGuildException("Construction called twice, that is not allowed.");
            }

            Console.WriteLine("Populating data from the message backup.");


            BackupData recoverData = await RestoreDataStructures();
            _data = new BotDataStructure(recoverData);

            if (Settings.UsingExtensionBanTracking)
            {
                // Temporary: if the bds structure was not parsed from the backup, because it didn't exist, just create a new empty one.
                if (recoverData.bds == null)
                {
                    Console.WriteLine("Unable to restore the old ban data structure, creating an empty one.");
                    recoverData.bds = new Extensions.BanDataStructure();
                }

                _bt = new Extensions.BanTracking(_guilds, recoverData.bds);
            }

            if (Settings.UsingExtensionRoleHighlights)
            {
                _highlighter = new Extensions.MainHighlighter(_guilds);
            }

            Console.WriteLine("Loaded " + _data.DiscordUplay.Count + " discord -- uplay connections.");
            Console.WriteLine("Loaded " + _data.QuietPlayers.Count + " players who wish not to be pinged.");
            Console.WriteLine("Loaded " + _data.DiscordRanks.Count + " current player ranks.");

            constructionComplete = true;
        }

        public async Task LoudenUserAndAddRoles(ulong discordID)
        {
            await _data.MakePlayerLoud(discordID);
            Rank r = await _data.QueryRank(discordID);
            foreach (DiscordGuild g in _guilds.byID.Values)
            {
                if (g.IsGuildMember(discordID))
                {
                    SocketGuildUser user = g._socket.Users.FirstOrDefault(x => x.Id == discordID);
                    await g.AddLoudRoles(user, r);
                }
            }
        }

        public async Task QuietenUserAndTakeRoles(ulong discordID)
        {
            await _data.ShushPlayer(discordID);
            foreach (DiscordGuild g in _guilds.byID.Values)
            {
                if (g.IsGuildMember(discordID))
                {
                    SocketGuildUser user = g._socket.Users.FirstOrDefault(x => x.Id == discordID);
                    await g.RemoveLoudRoles(user);
                }
            }
        }


        public async Task<BackupGuildConfiguration> RestoreGuildConfiguration()
        {
            // First, check that the primary guild is already loaded.
            if (!_primaryServerLoaded)
            {
                throw new PrimaryGuildException("Primary guild (Discord server) did not load and yet RestoreGuildConfiguration() is called.");
            }

            BackupGuildConfiguration gc = null;
            // First, attempt to restore the configuration from a message.

            var primaryChannel = _primary._socket.TextChannels.Single(ch => ch.Name == Settings.PrimaryConfigurationChannel);

            if (primaryChannel == null)
            {
                throw new PrimaryGuildException("Unable to find the primary configuration channel. Even if you are trying to restore from a file, create this channel first.");
            }

            var messages = primaryChannel.GetMessagesAsync().Flatten();
            var msgarray = await messages.ToArrayAsync();
            if (msgarray.Count() != 1)
            {
                Console.WriteLine($"Restoration expects exactly one message, found {msgarray.Count()}.");
            }
            else
            {
                var client = new HttpClient();
                var dataString = await client.GetStringAsync(msgarray[0].Attachments.First().Url);
                // TODO: exception handling here.
                gc = BackupGuildConfiguration.RestoreFromString(dataString);
            }

            // If the above fails, attempt to read it from a backup file.
            if (gc == null)
            {
                gc = BackupGuildConfiguration.RestoreFromFile(Settings.PrimaryConfigurationFile);
                if (gc == null)
                {
                    throw new PrimaryGuildException("Unable to restore guild configuration from any backup.");
                }
            }
            return gc;
        }

        public async Task<BackupData> RestoreDataStructures()
        {
            // First, check that the primary guild is already loaded.
            if (!_primaryServerLoaded)
            {
                throw new PrimaryGuildException("Primary guild (Discord server) did not load and yet RestoreGuildConfiguration() is called.");
            }

            BackupData bd = null;
            SocketTextChannel backupChannel = _primary._socket.TextChannels.Single(ch => ch.Name == Settings.PrimaryGuildBackupChannel);
            if (backupChannel == null)
            {
                throw new PrimaryGuildException("Unable to find the primary data backup channel. Even if you are trying to restore from a file, create this channel first.");
            }

            var messages = backupChannel.GetMessagesAsync().Flatten();
            var msgarray = await messages.ToArrayAsync();
            if (msgarray.Count() != 1)
            {
                Console.WriteLine($"Restoration expects exactly one message, found {msgarray.Count()}.");
            }
            else
            {
                var client = new HttpClient();
                var dataString = await client.GetStringAsync(msgarray[0].Attachments.First().Url);
                // TODO: exception handling here.
                bd = BackupData.RestoreFromString(dataString);
            }

            // If the above fails, attempt to read it from a backup file.
            if (bd == null)
            {
                bd = BackupData.RestoreFromFile(Settings.backupFile);
                if (bd == null)
                {
                    throw new PrimaryGuildException("Unable to restore guild configuration from any backup.");
                }
            }
            return bd;
        }

        /// <summary>
        /// Fetches a user when given the ID of a guild (server) they are in as well as their discord name/nickname.
        /// </summary>
        /// <param name="discordName"></param>
        /// <param name="guildID"></param>
        /// <returns></returns>
        public SocketGuildUser GetGuildUser(string discordName, ulong guildID)
        {
            if (!_guilds.byID.ContainsKey(guildID))
            {
                throw new GuildStructureException("We have tried to query a discord guild ID that doesn't exist.");
            }
            return _guilds.byID[guildID]._socket.Users.FirstOrDefault(x => ((x.Username == discordName) || (x.Nickname == discordName)));
        }


        public async Task UpdateRoles(ulong discordID, Rank newRank)
        {
            // Ignore everything until dwrap.ResidentGuild is set.
            if (!roleInitComplete || !constructionComplete)
            {
                return;
            }

            bool userDoNotDisturb = await _data.QueryQuietness(discordID);
            foreach (DiscordGuild guild in _guilds.byID.Values)
            {

                await guild.UpdateRoles(discordID, newRank, userDoNotDisturb);
            }
        }

        // Call RefreshRank() only when you hold the mutex to the internal dictionaries.
        private async Task RefreshRank(ulong discordID, string r6TabID)
        {
            try
            {
                TrackerDataSnippet data = await TRNHttpProvider.UpdateAndGetData(r6TabID);
                Rank fetchedRank = data.ToRank();

                bool updateRequired = true;
                if (await _data.TrackingContains(discordID))
                {
                    Rank curRank = await _data.QueryRank(discordID);
                    if (curRank.Equals(fetchedRank))
                    {
                        updateRequired = false;
                    }
                    else
                    {
                        Console.WriteLine("The fetched rank and the stored rank disagree for the user " + discordID);
                        Console.WriteLine($"The fetched rank equals {fetchedRank} and the stored rank is {curRank}.");
                    }
                }
                else
                {
                    Console.WriteLine("The user with DiscordID " + discordID + " is not yet in the database of ranks.");
                }

                if (updateRequired)
                {
                    await _data.UpdateRanks(discordID, fetchedRank);
                    await UpdateRoles(discordID, fetchedRank);
                }
                else
                {
                    // System.Console.WriteLine("Ranks match for player " + player.Username);
                }
            }
            catch (RankParsingException)
            {
                Console.WriteLine("Failed to update rank for player " + discordID);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine("Network unrechable, delaying update.");
                return;
            }
        }

        public async Task SyncRankRolesAndData()
        {
            // Note: we currently do not take access locks for the data structure.
            // We only touch it in a read-only way, so it should be okay.
            // This function should only be called at the initialization time anyway.
            int updates = 0;
            int preserved = 0;

            foreach( (var discordID, var uplayID) in _data.DiscordUplay)
            {
                foreach (var guild in _guilds.byID.Values)
                {
                    SocketGuildUser player = guild._socket.Users.FirstOrDefault(x => x.Id == discordID);
                    if (player == null)
                    {
                        continue;
                    }

                    List<string> allRoles = player.Roles.Select(x => x.Name).ToList();
                    Rank guessedRank = Ranking.GuessRank(allRoles);
                    Rank queriedRank = await _data.QueryRank(discordID);

                    if (!guessedRank.Equals(queriedRank))
                    {
                        try
                        {
                            Console.WriteLine($"Guessed and queried rank of user {player.Username} on guild {guild.GetName()} do not match, guessed: ({guessedRank.FullPrint()},{guessedRank.level}), queried: ({queriedRank.FullPrint()},{queriedRank.level})");
                            await UpdateRoles(discordID, queriedRank);
                        }
                        catch (RankParsingException)
                        {
                            Console.WriteLine($"Failed to fix mismatch for Discord user {player.Username}");
                        }
                        updates++;
                    }
                    else
                    {
                        preserved++;
                    }
                }
                // TODO: Possibly erase from the DB if the user IS null.
            }
            System.Console.WriteLine($"Bootstrap: {updates} players have their roles updated, {preserved} have the same roles.");
            roleInitComplete = true;
        }

        public async Task<bool> UpdateOne(ulong discordID)
        {
            // Ignore everything until the API connection to all guilds is ready.
            if (_guilds == null || !roleInitComplete)
            {
                return false;
            }


            if (! await _data.MappingContains(discordID))
            {
                return false;
            }

            string uplayId = await _data.QueryMapping(discordID);
            await RefreshRank(discordID, uplayId);
            return true;
        }

        public async Task UpdateAll()
        {

            // Ignore everything until the API connection to all guilds is ready.
            if (_guilds == null || !roleInitComplete)
            {
                return;
            }

            // Mild TODO: We can take into considerations how much do we hold the lock here.

            System.Console.WriteLine("Updating player ranks (period:" + Settings.updatePeriod.ToString() + ").");

            // We create a copy of the discord -- uplay mapping so that we can iterate without holding the lock.
            Dictionary<ulong, string> duplicateDiscordUplay = await _data.DuplicateUplayMapping();
            int count = 0;

            foreach (KeyValuePair<ulong, string> entry in duplicateDiscordUplay)
            {
                await RefreshRank(entry.Key, entry.Value);
                count++;
                // TODO: Possibly erase from the DB if the user IS null.
            }
            System.Console.WriteLine("Checked or updated " + count + " users.");
        }


        public async Task PerformBackup()
        {
            BackupData backup = await _data.PrepareBackup();

            // We have the backup data now, we can continue without the lock, as long as this was indeed a deep copy.
            Console.WriteLine($"Saving backup data to {Settings.backupFile}.");
            backup.BackupToFile(Settings.backupFile);

            // Additionally, write the backup to Discord itself, so we can bootstrap from the Discord server itself and don't need any local files.
            SocketTextChannel backupChannel = _primary._socket.TextChannels.SingleOrDefault(ch => ch.Name == Settings.PrimaryGuildBackupChannel);
            if (backupChannel != null)
            {
                // First, delete the previous backup. (This is why we also have a secondary backup.)
                var messages = backupChannel.GetMessagesAsync().Flatten();
                var msgarray = await messages.ToArrayAsync();
                if (msgarray.Count() > 1)
                {
                    Console.WriteLine($"The bot wishes not to delete only 1 message, found {msgarray.Count()}.");
                }

                if (msgarray.Count() == 1)
                {
                    await backupChannel.DeleteMessageAsync(msgarray[0]);
                }

                // Now, upload the new backup.
                await backupChannel.SendFileAsync(Settings.backupFile, $"Backup file rsixbot.json created at {DateTime.Now.ToShortTimeString()}.");
            }
        }

        /// <summary>
        /// A simple wrapper for UpdateAll() and BackupMappings() that makes sure both are called in one thread.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateAndBackup(object _)
        {
            await UpdateAll();
            await PerformBackup();
        }

        public string get_botStatus()
        {
            return botStatus;
        }
        public void set_botStatus(string txt)
        {
            botStatus = txt;
        }

        public async Task RunBotAsync()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
            client.Log += Log;
            await RegisterCommandsAsync();
            await client.LoginAsync(Discord.TokenType.Bot, Secret.botToken);
            await client.StartAsync();
            await client.SetGameAsync(get_botStatus());
            // We wish to set the bot's resident Discord guild and initialize.
            // This is however only possible when the client is ready.
            // See https://docs.stillu.cc/guides/concepts/events.html for further documentation.

            client.Ready += async () =>
            {
                if (!constructionComplete)
                {
                    await DelayedConstruction();
                }
                if (!roleInitComplete)
                {
                    _ = SyncRankRolesAndData();
                }

                return;
            };

            if (Settings.UsingExtensionRoleHighlights)
            {
                client.MessageReceived += _highlighter.Filter;
            }

            // Timer updateTimer = new Timer(new TimerCallback(UpdateAndBackup), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(60));
            Timer banTimer = new Timer(new TimerCallback(_bt.UpdateStructure), null, TimeSpan.FromMinutes(2), TimeSpan.FromHours(12));
            while (true)
            {
                // We do the big update also in a separate thread.
                await Task.Delay(Settings.updatePeriod);
                _ = UpdateAndBackup(null);
            }
            // await Task.Delay();
        }

        public void TestMessage(Object stateInfo)
        {
            Console.WriteLine($"{DateTime.Now}: Testing message.");
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
        public async Task RegisterCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message is null || message.Author.IsBot) return;
            if (!Settings.CommandChannels.Contains(message.Channel.Name)) return; // Ignore all channels except the allowed channel.
            int argPos = 0;

            if (message.HasStringPrefix("!", ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(client, message);
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
            }
            await client.SetGameAsync(get_botStatus());
        }


        static async Task Main(string[] args)
        {

            // TESTS
            /*
            string tester;
            R6TabDataSnippet snippet;
            string TRNID;
            Rank r;

            // Test TRN 0
            tester = "NotOrsonWelles";
            TRNID = await TRNHttpProvider.GetID(tester);
            Console.WriteLine(tester + "'s ID:" + TRNID);
            snippet = await TRNHttpProvider.GetData(TRNID);
            r = snippet.ToRank();
            Console.WriteLine(tester + "'s rank:" + r.FullPrint());

            // Test TRN 1
            string tester1 = "Lopata_6";
            string TRNID1 = await TRNHttpProvider.GetID(tester1);
            Console.WriteLine(tester1 + "'s ID:" + TRNID1);
            snippet = await TRNHttpProvider.GetData(TRNID1);
            r = snippet.ToRank();
            Console.WriteLine(tester1 + "'s rank:" + r.FullPrint());

            // Console.WriteLine(tester + "'s ID:" + testerID);
            // R6TabDataSnippet snippet = await R6Tab.GetData(testerID);
            // Rank r = snippet.ToRank();
            // Console.WriteLine(tester + "'s rank:" + r.FullPrint());
            // Test 2

            // List<string> darthList = new List<string> {"@everyone", "G", "Raptoil", "Stamgast", "Gold 2", "Gold", "G2" };
            // Rank guessDarth = Ranking.GuessRank(darthList);
            // if (!guessDarth.Equals(new Rank(Metal.Gold, 2)))
            // {
            //     Console.WriteLine("Sanity check failed. Darth's guess is" + guessDarth.FullPrint());
            //     throw new Exception();
            // }

            // Test TRN
            // string tester2 = "NotOrsonWelles";
            string TRNID2 = "dd33228f-5c0a-4e56-a7c6-6dc87d8bb3da";
            snippet = await TRNHttpProvider.GetData(TRNID2);


            // Test TRN Rankless (currently)
            Console.WriteLine("Test rankless:");
            // string tester3 = "Superzrout";
            string TRNID3 = "90520cd6-9fe2-4763-b250-b0333dd82158";
            snippet = await TRNHttpProvider.GetData(TRNID3);


            tester = "DandoStarris";
            TRNID = await TRNHttpProvider.GetID(tester);
            Console.WriteLine(tester + "'s ID:" + TRNID);
            snippet = await TRNHttpProvider.GetData(TRNID);
            r = snippet.ToRank();
            Console.WriteLine(tester + "'s rank:" + r.FullPrint());

             */ // END TESTS

            /* StatsDB b = new StatsDB();
            (bool DuoOrsonBanned, int OrsBanCode) = b.CheckUserBan("dd33228f-5c0a-4e56-a7c6-6dc87d8bb3da"); // DuoDoctorOrson
            (bool TeenagersBanned, int TeenBanCode) = b.CheckUserBan("71f7dd7b-fae0-4341-8788-c00085a7963d"); // Some teenagers guy, current ban: toxic behavior.
            (bool MartyBanned, int MartyBanCode) = b.CheckUserBan("6bc4610c-4ad4-4ee0-8173-284677e3140b"); // Marty.GLS
            Console.WriteLine($"Orson {DuoOrsonBanned} with code {OrsBanCode}, teenagers {TeenagersBanned} with code {TeenBanCode}, Marty {MartyBanned} with code {MartyBanCode}"); */
            await new Bot().RunBotAsync();

        }
    }
}