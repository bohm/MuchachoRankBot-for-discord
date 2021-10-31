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
        public DiscordWrapper dwrap;
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private bool initComplete = false;
        private bool dataReady = false;
        private bool _primaryServerLoaded = false;
        private string botStatus = Settings.botStatus;

        public BotDataStructure data;

        // The primary guild, used for backing up data as well as loading configuration data about other guilds.
        private PrimaryDiscordGuild _primary;
        // The other guilds (possibly including the primary one) where tracking and interfacing with users take place.
        private List<DiscordGuild> _guildList;


        public static int RoleNameIndex(string Name, string[] RoleNames)
        {
            for (int i = 0; i < RoleNames.Length; i++)
            {
                if (Name == RoleNames[i])
                {
                    return i;
                }

            }
            return -1;
        }



        public async Task AddLoudRoles(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketUser Author)
        {
            ulong DiscordID = Author.Id;
            Rank r = await data.QueryRank(DiscordID);
            await dwrap.AddLoudRoles(Guild, Author, r);
        }


        public async Task<BackupGuildConfiguration> RestoreGuildConfiguration()
        {
            // First, check that the primary guild is already loaded.
            if(!_primaryServerLoaded)
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
                gc = Backup.ConfigurationFromString(dataString);
            }

            // If this fails, attempt to read it from a backup file.
            Console.WriteLine("Primary guild loading did not go through, attempting to restore the configuration from a file.");

            gc = Backup.ConfigurationFromFile(Settings.PrimaryConfigurationFile);
            if (gc == null)
            {
                throw new PrimaryGuildException("Unable to restore guild configuration from any backup.");
            }
            return gc;
        }

        public async Task RestoreDataStructure()
        {

        }
        public async Task<BackupData> RestoreFromMessage()
        {
            if (dwrap.ResidentGuild == null)
            {
                return null;
            }

            BackupData bd = null;
            SocketTextChannel backupChannel = dwrap.ResidentGuild.TextChannels.Single(ch => ch.Name == Settings.PrimaryGuildBackupChannel);
            if (backupChannel != null)
            {
                var messages = backupChannel.GetMessagesAsync().Flatten();
                var msgarray = await messages.ToArrayAsync();
                if (msgarray.Count() != 1)
                {
                    Console.WriteLine($"Restoration expects exactly one message, found {msgarray.Count()}.");
                }

                var client = new HttpClient();
                var dataString = await client.GetStringAsync(msgarray[0].Attachments.First().Url);
                // TODO: exception handling here.
                bd = BackupData.RestoreFromString(dataString);
            }
            return bd;
        }


        /// <summary>
        /// Fills in the data fields from some backup. Previously we did that on Bot() initialization,
        /// we do that in a separate function now.
        /// </summary>
        /// <returns></returns>
        public async Task PopulateData()
        {
            if (dwrap.ResidentGuild == null)
            {
                return;
            }

            if (dataReady)
            {
                return;
            }

            Console.WriteLine("Populating data from the message backup.");

            // Try to deserialize the backup file first; if not found, initialize new structures.
            try
            {
                BackupData oldFile = await RestoreFromMessage();
                // BackupData oldFile = Backup.RestoreFromFile(settings.backupFile);
                DiscordUplay = oldFile.discordUplayDict;
                DiscordRanks = oldFile.discordRanksDict;
                QuietPlayers = oldFile.quietSet;

                if (Settings.UsingExtensionBanTracking)
                {
                    // Temporary: if the bds structure was not parsed from the backup, because it didn't exist, just create a new empty one.
                    if (oldFile.bds == null)
                    {
                        Console.WriteLine("Unable to restore the old ban data structure, creating an empty one.");
                        oldFile.bds = new Extensions.BanDataStructure();
                    }
                    bt.DelayedInit(oldFile.bds);
                }
            }
            catch (Exception)
            {
                System.Console.WriteLine("Failed to load the backup file, starting from scratch.");
            }

            if (DiscordUplay == null)
            {
                DiscordUplay = new Dictionary<ulong, string>();
            }
            else
            {
                System.Console.WriteLine("Loaded " + DiscordUplay.Count + " discord -- uplay connections.");
            }

            if (QuietPlayers == null)
            {
                Console.WriteLine("The quiet player file does not exist or did not load correctly.");
                QuietPlayers = new HashSet<ulong>();
            }
            else
            {
                System.Console.WriteLine("Loaded " + QuietPlayers.Count + " players who wish not to be pinged.");
            }

            if (DiscordRanks == null)
            {
                Console.WriteLine("The current rank state file does not exist or did not load correctly.");
                DiscordRanks = new Dictionary<ulong, Rank>();
            }
            else
            {
                System.Console.WriteLine("Loaded " + DiscordRanks.Count + " current player ranks.");
            }

            if (DiscordRanks.Count == 0)
            {
                // Query R6Tab to populate DiscordRanks.
                foreach (var (discordID, uplayID) in DiscordUplay)
                {
                    try
                    {
                        TrackerDataSnippet data = TRNHttpProvider.UpdateAndGetData(uplayID).Result;
                        Rank fetchedRank = data.ToRank();
                        DiscordRanks[discordID] = data.ToRank();
                        System.Console.WriteLine("Discord user " + discordID + " is fetched to have rank " + data.ToRank().FullPrint());
                    }
                    catch (RankParsingException)
                    {
                        System.Console.WriteLine("Failed to set rank (first run) for player " + discordID);
                    }
                }
            }
            dataReady = true;
        }

        public Bot()
        {
            Instance = this;
            data = new BotDataStructure();

        }


        // Creates all roles that the bot needs and which have not been created manually yet.
        public async Task PopulateRoles()
        {
            if (dwrap.ResidentGuild == null)
            {
                return;
            }

            // First add spectral metal roles, then spectral digit roles.
            foreach (string roleName in Ranking.SpectralMetalRoles)
            {
                var sameNameRole = dwrap.ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await dwrap.ResidentGuild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
                }
            }

            foreach (string roleName in Ranking.SpectralDigitRoles)
            {
                var sameNameRole = dwrap.ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await dwrap.ResidentGuild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
                }
            }

            // In the ordering, then come big loud roles and tiny loud roles.
            foreach (string roleName in Ranking.LoudMetalRoles)
            {
                var sameNameRole = dwrap.ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await dwrap.ResidentGuild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
                }
            }

            foreach (string roleName in Ranking.LoudDigitRoles)
            {
                var sameNameRole = dwrap.ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await dwrap.ResidentGuild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
                }
            }
        }

        public async Task UpdateRoles(ulong discordID, Rank newRank)
        {
            // Ignore everything until dwrap.ResidentGuild is set.
            if (dwrap.ResidentGuild == null)
            {
                return;
            }

            SocketGuildUser player = dwrap.ResidentGuild.Users.FirstOrDefault(x => x.Id == discordID);
            if (player == null)
            {
                // The player probably left the Discord guild. Just continue for now.
                throw new RankParsingException();
            }

            await dwrap.ClearAllRanks(player);

            if (QuietPlayers.Contains(player.Id))
            {
                Console.WriteLine("Updating spectral roles only for player " + player.Username);
                await dwrap.AddSpectralRoles(dwrap.ResidentGuild, player, newRank);
            }
            else
            {
                Console.WriteLine("Updating all roles only for player " + player.Username);
                await dwrap.AddSpectralRoles(dwrap.ResidentGuild, player, newRank);
                await dwrap.AddLoudRoles(dwrap.ResidentGuild, player, newRank);
            }
        }

        // Call RefreshRank() only when you hold the mutex to the internal dictionaries.
        private async Task RefreshRank(ulong discordID, string r6TabID)
        {
            // Ignore everything until dwrap.ResidentGuild is set.
            if (dwrap.ResidentGuild == null)
            {
                return;
            }

            // Discord.WebSocket.SocketGuildUser user = dwrap.ResidentGuild.Users.FirstOrDefault(x => x.Id == entry.Key);
            try
            {
                TrackerDataSnippet data = await TRNHttpProvider.UpdateAndGetData(r6TabID);
                Rank fetchedRank = data.ToRank();

                bool updateRequired = true;
                if (DiscordRanks.ContainsKey(discordID))
                {
                    Rank curRank = DiscordRanks[discordID];
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
                    await InsertIntoRanks(discordID, fetchedRank);
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

        public async Task RoleInit()
        { 
            await Access.WaitAsync();
            int updates = 0;
            int preserved = 0;

            foreach (var (discordID, uplayID) in DiscordUplay)
            {
                SocketGuildUser player = dwrap.ResidentGuild.Users.FirstOrDefault(x => x.Id == discordID);
                if (player == null)
                {
                    continue;
                }

                List<string> allRoles = player.Roles.Select(x => x.Name).ToList();
                Rank guessedRank = Ranking.GuessRank(allRoles);
                Rank queriedRank = DiscordRanks[discordID];

                if (!guessedRank.Equals(queriedRank))
                {
                    try
                    {
                        Console.WriteLine("Guessed and queried rank do not match, guessed: (" + guessedRank.FullPrint() + "," + guessedRank.level + "), queried: (" + queriedRank.FullPrint() + "," + queriedRank.level + ")");
                        await UpdateRoles(discordID, queriedRank);
                    }
                    catch (RankParsingException)
                    {
                        Console.WriteLine("Failed to fix mismatch for Discord user " + discordID);
                    }
                    updates++;
                }
                else
                {
                    preserved++;
                }
                // TODO: Possibly erase from the DB if the user IS null.
            }
            System.Console.WriteLine("Bootstrap: " + updates + " players have their roles updated, " + preserved + "have the same roles.");
            Access.Release();
            initComplete = true;
        }

    public async Task<bool> UpdateOne(ulong discordId)
    {
        // Ignore everything until dwrap.ResidentGuild is set
        if (dwrap.ResidentGuild == null || !initComplete)
        {
            return false;
        }

        await Access.WaitAsync();

        if (!DiscordUplay.ContainsKey(discordId))
        {
            return false;
        }

        string uplayId = DiscordUplay[discordId];
        await RefreshRank(discordId, uplayId);

        Access.Release();

        return true;
    }   

    public async Task UpdateAll()
        {

            // Ignore everything until dwrap.ResidentGuild is set
            if (dwrap.ResidentGuild == null || !initComplete)
            {
                return;
            }

            // We add a timer here and simply not update this time if the lock is held by some other
            // main function for longer than a second.
            bool access = await Access.WaitAsync(Settings.lockTimeout);
            if (!access)
            {
                System.Console.WriteLine("Pausing the update, a thread held the lock longer than " + Settings.lockTimeout.ToString());
                return;
            }

            System.Console.WriteLine("Updating player ranks (period:" + Settings.updatePeriod.ToString() + ").");
            int count = 0;

            foreach (KeyValuePair<ulong, string> entry in DiscordUplay)
            {
                await RefreshRank(entry.Key, entry.Value);
                count++;
                // TODO: Possibly erase from the DB if the user IS null.
            }
            System.Console.WriteLine("Checked or updated " + count + " users.");
            Access.Release();
        }


    public async Task PerformBackup()
        {
            BackupData backup = await data.PrepareBackup();

            // We have the backup data now, we can continue without the lock, as long as this was indeed a deep copy.
            Console.WriteLine($"Saving backup data to {Settings.backupFile}.");
            backup.BackupToFile(Settings.backupFile);

            // Additionally, write the backup to Discord itself, so we can bootstrap from the Discord server itself and don't need any local files.
            SocketTextChannel backupChannel = dg._socket.TextChannels.SingleOrDefault(ch => ch.Name == Settings.PrimaryGuildBackupChannel);
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
    }
        
        /// <summary>
        /// A simple wrapper for UpdateAll() and BackupMappings() that makes sure both are called in one thread.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateAndBackup(object _)
        {
            await UpdateAll();
            await BackupMappings();
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
                this.dwrap.ResidentGuild = client.GetGuild(Settings.residenceID);
                Console.WriteLine("Setting up residence in Discord guild " + this.dwrap.ResidentGuild.Name);
                if (!dataReady)
                {
                    await PopulateData();
                }
                if (!initComplete)
                {
                    _ = RoleInit();
                }

                _rh.DelayedInit();
                return;
            };

            if (Settings.UsingExtensionRoleHighlights)
            {
                client.MessageReceived += _rh.Filter;
            }

            // Timer updateTimer = new Timer(new TimerCallback(UpdateAndBackup), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(60));
            Timer banTimer = new Timer(new TimerCallback(bt.UpdateStructure), null, TimeSpan.FromMinutes(2), TimeSpan.FromHours(12));
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