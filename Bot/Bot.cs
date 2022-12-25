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

using RankBot.Extensions;
using RankBot.Commands;

namespace RankBot
{
    partial class Bot
    {
        public static Bot Instance;
        public bool constructionComplete = false;

        // public DiscordWrapper dwrap;
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private bool roleInitComplete = false;
        private bool _primaryServerLoaded = false;
        private string botStatus = Settings.botStatus;

        public UbisoftApi uApi;
        public BotDataStructure _data;

        // The primary guild, used for backing up data as well as loading configuration data about other guilds.
        private PrimaryDiscordGuild _primary = null;
        // The other guilds (possibly in cluding the primary one) where tracking and interfacing with users take place.
        // private List<DiscordGuild> _guildList;
        public DiscordGuilds guilds;

        // Parameters used by extensions, might be null if extensions are turned off.
        private Extensions.MainHighlighter _highlighter;
        public Extensions.LifetimeMmr MmrManager;

        // Command handler.
        private CommandManagement _mgmt;

        public Extensions.BanTracking bt;

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
                bt = new Extensions.BanTracking(guilds);
            }

            if (Settings.UsingExtensionRoleHighlights)
            {
                _highlighter = new Extensions.MainHighlighter(guilds);
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

            Console.WriteLine("Restoring guild list configuration from message backup.");
            BackupGuildConfiguration bgc = await RestoreGuildConfiguration();
            guilds = new DiscordGuilds(bgc, client);

            // New command manager.
            _mgmt = new CommandManagement();
            // We check for role creation here, instead of inside the constructor of DiscordGuild.
            // We do this not to make the constructor async itself. It might be better to check there.
            foreach (DiscordGuild g in guilds.byID.Values)
            {
                await g.RolePresenceCheckAsync();

                // We also register commands here. This does not need to run every time.
                if (Settings.RegenerateSlashCommands)
                {
                    await _mgmt.CreateGuildCommandsAsync(client, g);
                }
            }

            client.SlashCommandExecuted += _mgmt.SlashCommandHandlerAsync;

            Console.WriteLine("Populating data from the message backup.");


            BackupData recoverData = await RestoreDataStructures();
            _data = new BotDataStructure(recoverData);

            Console.WriteLine("Loaded " + _data.DiscordUplay.Count + " discord -- uplay connections.");
            Console.WriteLine("Loaded " + _data.QuietPlayers.Count + " players who wish not to be pinged.");
            Console.WriteLine("Loaded " + _data.DiscordRanks.Count + " current player ranks.");

            MmrManager = new LifetimeMmr();
            await MmrManager.DelayedInit(_primary);

            constructionComplete = true;
        }

        public async Task LoudenUserAndAddRoles(ulong discordID)
        {
            await _data.MakePlayerLoud(discordID);
            Rank r = await _data.QueryRank(discordID);
            foreach (DiscordGuild g in guilds.byID.Values)
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
            foreach (DiscordGuild g in guilds.byID.Values)
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
                throw new PrimaryGuildException("Primary guild (Discord server) did not load and yet RestoreDataStructures() is called.");
            }

            BackupData bd = null;
            SocketTextChannel backupChannel = _primary._socket.TextChannels.Single(ch => ch.Name == Settings.DataBackupChannel);
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

        public async Task TrackUser(DiscordGuild g, ulong discordID, string uplayNickname, string relevantChannelName)
        {
            try
            {
                if (!uApi.Online)
                {
                    await g.ReplyToUser("Ubisoft API aktualne neni dostupne a prikaz tedy nefunguje.", relevantChannelName, discordID);
                    return;
                }

                // SocketGuildUser trackedPerson = g.GetSingleUser(discordID);
                string queryR6ID = await _data.QueryMapping(discordID);
                if (queryR6ID != null)
                {
                    await g.ReplyToUser("Vas discord ucet uz sledujeme. / We are already tracking your Discord account.", relevantChannelName, discordID);
                    return;
                }

                string r6TabId = await uApi.GetID(uplayNickname);

                if (r6TabId == null)
                {
                    await g.ReplyToUser("Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.", relevantChannelName, discordID);
                    return;
                }
                await _data.InsertIntoMapping(discordID, r6TabId);
                await g.ReplyToUser($"Nove sledujeme vase uspechy pod prezdivkou {uplayNickname} na platforme PC. / We now track you as {uplayNickname} on PC.", relevantChannelName, discordID);

                // Update the newly added user.

                bool ret = await Bot.Instance.UpdateOne(discordID);

                if (ret)
                {
                    // Print user's rank too.
                    Rank r = await uApi.GetRank(r6TabId);
                    if (r.Digits())
                    {
                        await g.ReplyToUser($"Aktualne vidime vas rank jako {r.FullPrint()}", relevantChannelName, discordID);
                    }
                    else
                    {
                        await g.ReplyToUser($"Aktualne vidime vas rank jako {r.CompactFullPrint()}", relevantChannelName, discordID);
                    }
                }
                else
                {
                    await g.ReplyToUser("Stala se chyba pri nastaven noveho ranku.", relevantChannelName, discordID);
                }


            }
            catch (RankParsingException e)
            {
                await g.ReplyToUser("Communication to the R6Tab server failed. Please try again or contact the local Discord admins.", relevantChannelName, discordID);
                await g.Reply("Pro admina: " + e.Message, relevantChannelName);
            }
        }

        public async Task UpdateRoles(ulong discordID, Rank newRank)
        {
            // Ignore everything until dwrap.ResidentGuild is set.
            if (!constructionComplete)
            {
                return;
            }

            if (!uApi.Online)
            {
                return;
            }

            bool userDoNotDisturb = await _data.QueryQuietness(discordID);
            foreach (DiscordGuild guild in guilds.byID.Values)
            {
                if (guild.IsGuildMember(discordID))
                {
                    await guild.UpdateRoles(discordID, newRank, userDoNotDisturb);
                }
            }
        }

        // Call RefreshRank() only when you hold the mutex to the internal dictionaries.
        private async Task RefreshRank(ulong discordID, string r6TabID)
        {
            if (!uApi.Online)
            {
                Console.WriteLine("Ubisoft API not online, update skipped.");
                return;
            }

            try
            {

                Rank fetchedRank = await uApi.GetRank(r6TabID);

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


            if (!uApi.Online)
            {
                Console.WriteLine("Ubisoft API is offline, SyncRankRoles cannot proceed.");
                return;
            }

            foreach( (var discordID, var uplayID) in _data.DiscordUplay)
            {
                foreach (var guild in guilds.byID.Values)
                {
                    SocketGuildUser player = guild._socket.Users.FirstOrDefault(x => x.Id == discordID);
                    if (player == null)
                    {
                        continue;
                    }

                    List<string> allRoles = player.Roles.Select(x => x.Name).ToList();
                    Rank guessedRank = Ranking.GuessRank(allRoles);
                    Rank queriedRank = await _data.QueryRank(discordID);


                    // Attempt to solve the inconsistency in the database.
                    if (queriedRank == null)
                    {
                        Console.WriteLine($"The player {player.Username} does not have a stored rank, but has a uplay ID association. Fixing.");
                        UbisoftRank response = await uApi.QuerySingleRank(uplayID);
                        Rank actualRank = response.ToRank();
                        Console.WriteLine($"Fetched rank {actualRank.FullPrint()} for player {player.Username}");
                        await _data.UpdateRanks(discordID, actualRank);
                        queriedRank = actualRank;
                    }

                    if (!guessedRank.Equals(queriedRank))
                    {
                        try
                        {
                            Console.WriteLine($"Guessed and queried rank of user {player.Username} on guild {guild.GetName()} do not match.");
                            if(queriedRank != null && guessedRank != null)
                            {
                                Console.WriteLine($"guessed: ({guessedRank.FullPrint()},{guessedRank.level}), queried: ({queriedRank.FullPrint()},{queriedRank.level})");
                            }

                            UbisoftRank response = await uApi.QuerySingleRank(uplayID);
                            Rank actualRank = response.ToRank();
                            Console.WriteLine($"Fetched rank {actualRank.FullPrint()} for player {player.Username}");

                            if (!actualRank.Equals(queriedRank))
                            {
                                await _data.UpdateRanks(discordID, actualRank);
                            }
                            else
                            {
                                Console.WriteLine("Queried rank was correct, no need to update the DB.");
                            }

                            if (!actualRank.Equals(guessedRank))
                            {
                                await UpdateRoles(discordID, actualRank);
                            }
                            {
                                Console.WriteLine("Guessed rank was correct, no need to update roles.");
                            }
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
            if (guilds == null || !roleInitComplete || !uApi.Online)
            {
                return false;
            }


            if (! await _data.UserTracked(discordID))
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
            if (guilds == null || !roleInitComplete || !uApi.Online)
            {
                return;
            }

            System.Console.WriteLine("Updating player ranks (period:" + Settings.updatePeriod.ToString() + ").");

            // We create a copy of the discord -- uplay mapping so that we can iterate without holding the lock.
            Dictionary<ulong, string> duplicateDiscordUplay = await _data.DuplicateUplayMapping();
            int count = 0;

            foreach (KeyValuePair<ulong, string> entry in duplicateDiscordUplay)
            {
                await RefreshRank(entry.Key, entry.Value);
                count++;
                Thread.Sleep(1000); // Added not to overwhelm the API. Possibly change to a batched request system.
                // TODO: Possibly erase from the DB if the user IS null.
            }
            System.Console.WriteLine("Checked or updated " + count + " users.");
        }


        public async Task PerformBackup()
        {
            BackupData backup = await _data.PrepareBackup();
            if (Settings.UsingExtensionBanTracking)
            {
                bt.ExtendBackup(backup);
            }

            // We have the backup data now, we can continue without the lock, as long as this was indeed a deep copy.
            Console.WriteLine($"Saving backup data to {Settings.backupFile}.");
            backup.BackupToFile(Settings.backupFile);
            await _primary.BackupFileToMessage(Settings.backupFile, Settings.DataBackupChannel);
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
            if (message is null || message.Author.IsBot)
            {
                return;
            }

            var contextChannel = arg.Channel;
            if (contextChannel is SocketTextChannel guildChannel)
            {
                ulong gid = guildChannel.Guild.Id;
                if (!guilds.byID.ContainsKey(gid))
                {
                    // Console.WriteLine($"The service guilds do not contain the guild {gid}"); // DEBUG
                    return;
                }

                if (!guilds.byID[gid].Config.commandChannels.Contains(message.Channel.Name))
                {
                    // Console.WriteLine($"The message comes from a channel {message.Channel.Name}, which is not monitored."); // DEBUG
                    return; // Ignore all channels except one the allowed command channels.
                }

                int argPos = 0;

                if (message.HasStringPrefix("!", ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
                {
                    var context = new SocketCommandContext(client, message);
                    var result = await commands.ExecuteAsync(context, argPos, services);
                    if (!result.IsSuccess)
                        Console.WriteLine(result.ErrorReason);
                }
            }
            // await client.SetGameAsync(get_botStatus());
        }

        /// <summary>
        /// A pseudo unit test; should be decoupled into a full fledged test.
        /// Connects to the primary guild, restores the structures, prints some basic info and terminates.
        /// </summary>
        /// <returns></returns>
        public async Task TestBotAsync()
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
                if (_primary == null)
                {
                    _primary = new PrimaryDiscordGuild(client);
                    _primaryServerLoaded = true;
                }
                if (!constructionComplete)
                {
                    await DelayedConstruction();
                    if (Settings.UsingExtensionRoleHighlights)
                    {
                        client.MessageReceived += _highlighter.Filter;
                    }
                }
                return;
            };


            while (true)
            {
                if (constructionComplete)
                {
                    Console.WriteLine("Server list:");
                    int i = 0;
                    foreach (DiscordGuild dg in guilds.byID.Values)
                    {
                        Console.WriteLine($"{i++}: {dg.GetName()}");
                    }
                }
                await Task.Delay(Settings.updatePeriod);
            }
        }

        public async Task RunBotAsync()
        {

            uApi = new UbisoftApi();
            await uApi.DelayedInit();
            Timer banTimer = null;

            var config = new DiscordSocketConfig();
            config.GatewayIntents = GatewayIntents.All;

            client = new DiscordSocketClient(config);

            // client = new DiscordSocketClient();
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
                if (_primary == null)
                {
                    _primary = new PrimaryDiscordGuild(client);
                    _primaryServerLoaded = true;
                }
                if (!constructionComplete)
                {
                    await DelayedConstruction();
                }
                if (!roleInitComplete)
                {
                    // _ = SyncRankRolesAndData();
                }

                return;
            };


            // Timer updateTimer = new Timer(new TimerCallback(UpdateAndBackup), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(60));
            while (true)
            {
                // We do the big update also in a separate thread.
                await Task.Delay(Settings.updatePeriod);
                // _ = UpdateAndBackup(null);
            }
            // await Task.Delay();
        }

        static async Task Main(string[] args)
        {
            // TESTS

            // Testing new API.

            //UbisoftApi uApi = new UbisoftApi();
            //await uApi.DelayedInit();

            //string orsonUplay = "93f4f20f-ac19-47fb-afe8-f36662a40b79";
            //string orsonNickname = await uApi.GetCurrentUplay(orsonUplay);
            //Console.WriteLine($"The current Uplay nickname of {orsonUplay} is {orsonNickname}. ");
            //int orsonMmr = await uApi.GetCurrentMMR(orsonUplay);
            //Console.WriteLine($"The current MMR of {orsonNickname} is {orsonMmr}. ");

            //HashSet<string> severalIds = new HashSet<string> { "93f4f20f-ac19-47fb-afe8-f36662a40b79", "90520cd6-9fe2-4763-b250-b0333dd82158" };
            //UbisoftRankResponse severalResponses = await uApi.QueryMultipleRanks(severalIds);
            //foreach( (string uplayId, UbisoftRank rnk) in severalResponses.players)
            //{
            //    Console.WriteLine($"{uplayId}: {rnk.ToRank().CompactFullPrint()}.");
            //}


            // GuildConfigTest.Run();
            // await new Bot().TestBotAsync();

            // END TESTS

            // Full run:
            await new Bot().RunBotAsync();

        }
    }
}