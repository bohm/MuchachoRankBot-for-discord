using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
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

namespace R6RankBot
{
    class Bot
    {
        public static Bot Instance;
        // The guild we are operating on. We start doing things only once this is no longer null.
        public Discord.WebSocket.SocketGuild ResidentGuild;

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private bool initComplete = false;
        private bool dataReady = false;

        // The internal mapping between Discord names and R6TabIDs which we use to track ranks. Persists via backups.
        private Dictionary<ulong, string> DiscordUplay;
        // The internal data about ranks of Discord users. Does not persist on shutdown.
        private Dictionary<ulong, Rank> DiscordRanks;
        private HashSet<ulong> QuietPlayers;

        // The semaphore (in fact a mutex) that blocks the access to DoNotTrack and DiscordUplay
        private readonly SemaphoreSlim Access;


        public static bool IsOperator(ulong id)
        {
            return (settings.Operators.Contains(id));
        }

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

        // Remove all rank roles from a user.
        public static async Task ClearAllRanks(Discord.WebSocket.SocketGuildUser User)
        {
            System.Console.WriteLine("Removing all ranks from user " + User.Username);
            foreach (var RankRole in User.Roles.Where(x => Ranking.LoudMetalRoles.Contains(x.Name) || Ranking.LoudDigitRoles.Contains(x.Name)
                                                        || Ranking.SpectralMetalRoles.Contains(x.Name) || Ranking.SpectralDigitRoles.Contains(x.Name)
                                                        || Ranking.ChillRole == x.Name))
            {
                await User.RemoveRoleAsync(RankRole);
            }
        }
  
        public static async Task RemoveLoudRoles(Discord.WebSocket.SocketUser Author)
        {
            // If the user is in any mentionable rank roles, they will be removed.
            var Us = (Discord.WebSocket.SocketGuildUser)Author;
            // For each loud role, which can be a loud big role or a loud tiny role:
            foreach (var LoudRole in Us.Roles.Where(x => Ranking.LoudDigitRoles.Contains(x.Name) || Ranking.LoudMetalRoles.Contains(x.Name)))
            {
                await Us.RemoveRoleAsync(LoudRole);
            }
        }

        public static async Task AddLoudRoles(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketUser Author, Rank rank)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add the non-digit role.
            string nonDigitLoudName = rank.CompactMetalPrint();
            var LoudRole = Guild.Roles.FirstOrDefault(x => x.Name == nonDigitLoudName);
            if (LoudRole != null)
            {
                await Us.AddRoleAsync(LoudRole);
            }

            // Then, if the rank has a digit role, add that too.
            if (rank.Digits())
            {
                string digitLoudName = rank.CompactFullPrint();
                var LoudDigitRole = Guild.Roles.FirstOrDefault(x => x.Name == digitLoudName);
                if (LoudDigitRole != null)
                {
                    await Us.AddRoleAsync(LoudDigitRole);
                }
            }
        }

        public static async Task AddSpectralRoles(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketUser Author, Rank rank)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add the non-digit role.
            string nonDigitSpectralName = rank.SpectralMetalPrint();
            var spectralRole = Guild.Roles.FirstOrDefault(x => x.Name == nonDigitSpectralName);
            if (spectralRole != null)
            {
                await Us.AddRoleAsync(spectralRole);
            }

            // Then, if the rank has a digit role, add that too.
            if (rank.Digits())
            {
                string digitSpectralName = rank.SpectralFullPrint();
                var digitSpectralRole = Guild.Roles.FirstOrDefault(x => x.Name == digitSpectralName);
                if (digitSpectralRole != null)
                {
                    await Us.AddRoleAsync(digitSpectralRole);
                }
            }
        }

        public static async Task<Rank> GetCurrentRank(string R6TabID)
        {
            R6TabDataSnippet data = await TRNHttpProvider.GetData(R6TabID);
            Rank r = data.ToRank();
            return r;
        }
        // --- End of static section. ---

        public async Task CleanSlate()
        {
            if (ResidentGuild == null)
            {
                return;
            }

            foreach (SocketGuildUser user in ResidentGuild.Users)
            {
                await ClearAllRanks(user);
            }

            System.Console.WriteLine("The slate is now clean, no users have roles.");
            initComplete = true;
        }

        public async Task AddLoudRoles(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketUser Author)
        {
            ulong DiscordID = Author.Id;
            Rank r = await QueryRank(DiscordID);
            await AddLoudRoles(Guild, Author, r);
        }

        public async Task BackupMappings()
        {
            await Access.WaitAsync();
            BackupData data = new BackupData();
            data.discordRanksDict = new Dictionary<ulong, Rank>(DiscordRanks);
            data.discordUplayDict = new Dictionary<ulong, string>(DiscordUplay);
            data.quietSet = new HashSet<ulong>(QuietPlayers);
            Access.Release();
            // We have the data now, we can continue without the lock, as long as this was indeed a deep copy.

            Console.WriteLine($"Saving backup data to {settings.backupFile}.");
            Backup.BackupToFile(data, settings.backupFile);

            // Additionally, write the backup to Discord itself, so we can bootstrap from the Discord server itself and don't need any local files.
            var backupChannel = ResidentGuild.TextChannels.Single(ch => ch.Name == "rank-bot-backups");
            if (backupChannel != null)
            {
                // First, delete the previous backup. (This is why we also have a secondary backup.)
                var messages = backupChannel.GetMessagesAsync().Flatten();
                var msgarray = await messages.ToArray();
                if (msgarray.Count() > 1)
                {
                    Console.WriteLine($"The bot wishes not to delete only 1 message, found {msgarray.Count()}.");
                }

                if (msgarray.Count() == 1)
                {
                    await backupChannel.DeleteMessageAsync(msgarray[0]);
                }

                // Now, upload the new backup.
                await backupChannel.SendFileAsync(settings.backupFile, $"Backup file rsixbot.json created at {DateTime.Now.ToShortTimeString()}.");
            }
        }

        public async Task<BackupData> RestoreFromMessage()
        {
            if (ResidentGuild == null)
            {
                return null;
            }

            BackupData bd = null;
            var backupChannel = ResidentGuild.TextChannels.Single(ch => ch.Name == "rank-bot-backups");
            if (backupChannel != null)
            {
                // First, delete the previous backup. (This is why we also have a secondary backup.)
                var messages = backupChannel.GetMessagesAsync().Flatten();
                var msgarray = await messages.ToArray();
                if (msgarray.Count() != 1)
                {
                    Console.WriteLine($"Restoration expects exactly one message, found {msgarray.Count()}.");
                }

                var client = new HttpClient();
                var dataString = await client.GetStringAsync(msgarray[0].Attachments.First().Url);
                // TODO: exception handling here.
                bd = Backup.RestoreFromString(dataString);
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
            if (ResidentGuild == null)
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
            }
            catch (IOException)
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
                        R6TabDataSnippet data = TRNHttpProvider.UpdateAndGetData(uplayID).Result;
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

            Access = new SemaphoreSlim(1, 1);

        }


        // Creates all roles that the bot needs and which have not been created manually yet.
        public async Task PopulateRoles()
        {
            if (ResidentGuild == null)
            {
                return;
            }

            // First add spectral metal roles, then spectral digit roles.
            foreach (string roleName in Ranking.SpectralMetalRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }

            foreach (string roleName in Ranking.SpectralDigitRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }

            // In the ordering, then come big loud roles and tiny loud roles.
            foreach (string roleName in Ranking.LoudMetalRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }

            foreach (string roleName in Ranking.LoudDigitRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    System.Console.WriteLine("Populating role " + roleName);
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }
        }

        public async Task UpdateRoles(ulong discordID, Rank newRank)
        {
            // Ignore everything until ResidentGuild is set.
            if (ResidentGuild == null)
            {
                return;
            }

            SocketGuildUser player = ResidentGuild.Users.FirstOrDefault(x => x.Id == discordID);
            if (player == null)
            {
                // The player probably left the Discord guild. Just continue for now.
                throw new RankParsingException();
            }

            await ClearAllRanks(player);

            if (QuietPlayers.Contains(player.Id))
            {
                Console.WriteLine("Updating spectral roles only for player " + player.Username);
                await AddSpectralRoles(ResidentGuild, player, newRank);
            }
            else
            {
                Console.WriteLine("Updating all roles only for player " + player.Username);
                await AddSpectralRoles(ResidentGuild, player, newRank);
                await AddLoudRoles(ResidentGuild, player, newRank);
            }
        }

        // Call RefreshRank() only when you hold the mutex to the internal dictionaries.
        private async Task RefreshRank(ulong discordID, string r6TabID)
        {
            // Ignore everything until ResidentGuild is set.
            if (ResidentGuild == null)
            {
                return;
            }

            // Discord.WebSocket.SocketGuildUser user = ResidentGuild.Users.FirstOrDefault(x => x.Id == entry.Key);
            try
            {
                R6TabDataSnippet data = await TRNHttpProvider.UpdateAndGetData(r6TabID);
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
                SocketGuildUser player = ResidentGuild.Users.FirstOrDefault(x => x.Id == discordID);
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
        // Ignore everything until ResidentGuild is set
        if (ResidentGuild == null || !initComplete)
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

            // Ignore everything until ResidentGuild is set
            if (ResidentGuild == null || !initComplete)
            {
                return;
            }

            // We add a timer here and simply not update this time if the lock is held by some other
            // main function for longer than a second.
            bool access = await Access.WaitAsync(settings.lockTimeout);
            if (!access)
            {
                System.Console.WriteLine("Pausing the update, a thread held the lock longer than " + settings.lockTimeout.ToString());
                return;
            }

            System.Console.WriteLine("Updating player ranks (period:" + settings.updatePeriod.ToString() + ").");
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

        public async Task<string> QueryMapping(ulong discordId)
        {
            await Access.WaitAsync();

            string r6TabId = null;
            DiscordUplay.TryGetValue(discordId, out r6TabId);
            Access.Release();
            return r6TabId;
        }

        public async Task InsertIntoMapping(ulong discordId, string r6TabId)
        {
            await Access.WaitAsync();

            if (DiscordUplay.ContainsKey(discordId))
            {
                Access.Release();
                throw new DuplicateException();
            }

            DiscordUplay[discordId] = r6TabId;
            Access.Release();
        }

        public async Task<Rank> QueryRank(ulong DiscordID)
        {
            await Access.WaitAsync();
            Rank r;
            DiscordRanks.TryGetValue(DiscordID, out r);
            Access.Release();
            return r;
        }
        /// <summary>
        /// Inserts into the DiscordRanks internal dictionary of mapping from
        /// ids to ranks. Only call when having access to the database.
        /// </summary>
        /// <param name="discordID"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public async Task InsertIntoRanks(ulong discordID, Rank r)
        {
            // await Access.WaitAsync();
            DiscordRanks[discordID] = r;
            // Access.Release();
        }


        public async Task RemoveFromDatabases(ulong discordId)
        {
            await Access.WaitAsync();

            if (DiscordUplay.ContainsKey(discordId))
            {
                DiscordUplay.Remove(discordId);
            }

            if (DiscordRanks.ContainsKey(discordId))
            {
                DiscordRanks.Remove(discordId);
            }

            if (QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Remove(discordId);
            }

            Access.Release();
        }
        public async Task ShushPlayer(ulong discordId)
        {
            await Access.WaitAsync();
            if (!QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Add(discordId);
            }
            Access.Release();
        }

        public async Task MakePlayerLoud(ulong discordId)
        {
            await Access.WaitAsync();
            if (QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Remove(discordId);
            }
            Access.Release();
        }

        
        /// <summary>
        /// A simple wrapper for UpdateAll() and BackupMappings() that makes sure both are called in one thread.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateAndBackup()
        {
            await UpdateAll();
            await BackupMappings();
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
            await client.SetGameAsync(settings.get_botStatus());
            // We wish to set the bot's resident Discord guild and initialize.
            // This is however only possible when the client is ready.
            // See https://docs.stillu.cc/guides/concepts/events.html for further documentation.

            client.Ready += async () =>
            {
                this.ResidentGuild = client.GetGuild(settings.residenceID);
                Console.WriteLine("Setting up residence in Discord guild " + this.ResidentGuild.Name);
                if (!dataReady)
                {
                    await PopulateData();
                }
                if (!initComplete)
                {
                    _ = RoleInit();
                }
                return;
            };

            while (true)
            {
                // We do the big update also in a separate thread.
                await Task.Delay(settings.updatePeriod);
                _ = UpdateAndBackup();
            }
            // await Task.Delay();
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
            if (!settings.BotChannels.Contains(message.Channel.Name)) return; // Ignore all channels except the allowed channel.
            int argPos = 0;
                
            if (message.HasStringPrefix("!", ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(client, message);
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
            }
            await client.SetGameAsync(settings.get_botStatus());
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

            await new Bot().RunBotAsync();

        }
    }
}