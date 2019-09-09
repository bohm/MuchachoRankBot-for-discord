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

namespace DiscordBot
{
    class Bot
    {
        public static Bot Instance;
        // The guild we are operating on. We start doing things only once this is no longer null.
        public Discord.WebSocket.SocketGuild ResidentGuild;

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;


        // The internal mapping between Discord names and Uplay (or xbox) names which we use to track ranks.
        private Dictionary<ulong, Tuple<string, string>> DiscordUplay;
        // The internal mapping for people that should not be tracked.
        private HashSet<ulong> DoNotTrack;
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
            foreach (var RankRole in User.Roles.Where(x => settings.BigLoudRoles.Contains(x.Name) || settings.TinyLoudRoles.Contains(x.Name) || settings.BigQuietRoles.Contains(x.Name) || settings.TinyQuietRoles.Contains(x.Name)))
            {
                await User.RemoveRoleAsync(RankRole);
            }
        }

        // Add a specific rank to a user.
        // Only sets up quiet ranks -- if you want to set up all ranks, call AddLoudRoles() afterwards.
        public static async Task SetQuietRanks(Discord.WebSocket.SocketGuild guild, Discord.WebSocket.SocketGuildUser user, Tuple<int, int> roles)
        {
            // The big role should never be -1, but we still check it.
            if (roles.Item1 != -1)
            {
                string quietBigName = settings.BigQuietRoles[roles.Item1];
                var quietBigRole = guild.Roles.FirstOrDefault(x => x.Name == quietBigName);

                if (quietBigRole != null)
                {
                    await user.AddRoleAsync(quietBigRole);
                }
            }

            // The tiny role can be -1 (Unranked, Diamond, ...).
            if (roles.Item2 != -1)
            {
                string quietTinyName = settings.TinyQuietRoles[roles.Item2];
                var quietTinyRole = guild.Roles.FirstOrDefault(x => x.Name == quietTinyName);

                if (quietTinyRole != null)
                {
                    await user.AddRoleAsync(quietTinyRole);
                }
            }
        }

        public static async Task RemoveLoudRoles(Discord.WebSocket.SocketUser Author, Discord.WebSocket.SocketGuild Guild)
        {
            // If the user is in any mentionable rank roles, they will be removed.
            var Us = (Discord.WebSocket.SocketGuildUser)Author;
            // For each loud role, which can be a loud big role or a loud tiny role:
            foreach (var LoudRole in Us.Roles.Where(x => settings.BigLoudRoles.Contains(x.Name) || settings.TinyLoudRoles.Contains(x.Name)))
            {
                await Us.RemoveRoleAsync(LoudRole);
            }
        }

        public static async Task AddLoudRoles(Discord.WebSocket.SocketUser Author, Discord.WebSocket.SocketGuild Guild)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add big roles;
            foreach (var QuietRole in Us.Roles.Where(x => settings.BigQuietRoles.Contains(x.Name)))
            {
                int index = RoleNameIndex(QuietRole.Name, settings.BigQuietRoles);
                if (index != -1)
                {
                    string Corresponding = settings.BigLoudRoles[index];
                    var LoudRole = Guild.Roles.FirstOrDefault(x => x.Name == Corresponding);
                    if (LoudRole != null)
                    {
                        System.Console.WriteLine("Adding role " + LoudRole.Name);
                        await Us.AddRoleAsync(LoudRole);
                    }
                }
            }

            // Then add tiny roles.
            foreach (var QuietRole in Us.Roles.Where(x => settings.TinyQuietRoles.Contains(x.Name)))
            {
                int index = RoleNameIndex(QuietRole.Name, settings.TinyQuietRoles);
                if (index != -1)
                {
                    string Corresponding = settings.TinyLoudRoles[index];
                    var LoudRole = Guild.Roles.FirstOrDefault(x => x.Name == Corresponding);
                    if (LoudRole != null)
                    {
                        System.Console.WriteLine("Adding role " + LoudRole.Name);
                        await Us.AddRoleAsync(LoudRole);
                    }
                }
            }
        }

        // TODO: Implement region.
        public static async Task<Tuple<int, int>> GetCurrentRank(string Nick, string Region, string Platform)
        {
            switch (Platform.ToLower())
            {
                case "pc":
                    Platform = "uplay";
                    break;
                case "xbox":
                    Platform = "xbl";
                    break;
                case "ps4":
                    Platform = "psn";
                    break;
                default:
                    Platform = null;
                    break;
            }

            if (Platform == null)
            {
                throw new RankParsingException(); // TODO: IncorrectPlatformException
            }

            string url = "https://r6tab.com/api/search.php?platform=" + Platform + "&search=" + Nick;
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url);
            string source = null;
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                source = await response.Content.ReadAsStringAsync();
            }

            string subStringResult = "totalresults";
            string test = source.Substring(source.IndexOf(subStringResult) + 14, 1);
            if (Int32.Parse(test) != 0)
            {
                string subStringId = "p_id";
                source = source.Substring(source.IndexOf(subStringId) + 7, 36);
                url = "https://r6tab.com/api/player.php?p_id=" + source;
                client = new HttpClient();
                response = await client.GetAsync(url);
                source = null;
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    source = await response.Content.ReadAsStringAsync();
                }
                string subStringCurRank = "p_currentrank";
                string p_currentrank = null;
                p_currentrank = source.Substring(source.IndexOf(subStringCurRank) + 15, 2);

                if (Regex.IsMatch(p_currentrank.Substring(1), ","))
                {
                    p_currentrank = p_currentrank.Substring(0, 1);
                }

                if (source != null)
                {
                    int TabRank = -1;
                    if (!Int32.TryParse(p_currentrank, out TabRank))
                    {
                        throw new RankParsingException();
                    }

                    if (TabRank < 0 || TabRank >= settings.R6TabRanks.Length)
                    {
                        throw new RankParsingException();
                    }

                    // Get the big role -- e.g. Copper.
                    int BigRole = settings.BigRoleFromRank(TabRank);
                    // Get the little role -- e.g. Copper 3. May be -1.
                    int TinyRole = settings.TinyRoleFromRank(TabRank);

                    return new Tuple<int, int>(BigRole, TinyRole);
                }
                else
                {
                    throw new RankParsingException();
                }
            }
            else
            {
                throw new RankParsingException();
            }
        }

        // Infers rank from the roles the player is currently in.
        // Returns null when it could not figure the role.
        public Tuple<int, int> InferRankFromRoles(Discord.WebSocket.SocketGuildUser player)
        {
            // If the player has a chill role, give up.
            if (player.Roles.FirstOrDefault(x => x.Name == settings.ChillRole) != null)
            {
                System.Console.WriteLine("Found a Full Chill role, will not attempt to infer");
                return null;
            }

            var bigRole = player.Roles.FirstOrDefault(x => settings.BigQuietRoles.Contains(x.Name));
            if (bigRole == null)
            {
                return null;
            }

            int index = Array.IndexOf(settings.BigQuietRoles, bigRole.Name);
            if (index == -1)
            {
                return null;
            }

            // At this point, we have inferred a big role. Try to infer a small role.

            var tinyRole = player.Roles.FirstOrDefault(x => settings.TinyQuietRoles.Contains(x.Name));
            if (tinyRole == null)
            {
                return new Tuple<int,int>(index, -1);
            }

            int tinyIndex = Array.IndexOf(settings.TinyQuietRoles, tinyRole.Name);

            return new Tuple<int, int>(index, tinyIndex);
        }

        public async Task BackupMappings()
        {
            await Access.WaitAsync();
            JsonSerializer serializer = new JsonSerializer();

            using (StreamWriter sw = new StreamWriter(settings.serializeFile))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, DiscordUplay);
                serializer.Serialize(jw, DoNotTrack);
                serializer.Serialize(jw, QuietPlayers);
            }
            Access.Release();
        }

        public Bot()
        {
            Instance = this;
            Access = new SemaphoreSlim(1, 1);

            // Try to deserialize the backup file first; if not found, initialize new structures.
            if (File.Exists(settings.serializeFile))
            {
                try
                {
                    StreamReader file = File.OpenText(settings.serializeFile);
                    JsonSerializer serializer = new JsonSerializer();
                    DiscordUplay = (Dictionary<ulong, Tuple<string, string>>)serializer.Deserialize(file, typeof(Dictionary<ulong, Tuple<string, string>>));
                    DoNotTrack = (HashSet<ulong>)serializer.Deserialize(file, typeof(HashSet<ulong>));
                    QuietPlayers = (HashSet<ulong>)serializer.Deserialize(file, typeof(HashSet<ulong>));
                    file.Close();
                }
                catch (IOException)
                {
                    System.Console.WriteLine("Failed to load the backup file, starting from scratch.");
                }
            }

            if (DiscordUplay == null)
            {
                DiscordUplay = new Dictionary<ulong, Tuple<string, string>>();
            }

            if (DoNotTrack == null)
            {
                DoNotTrack = new HashSet<ulong>();
            }

            if (QuietPlayers == null)
            {
                QuietPlayers = new HashSet<ulong>();
            }
        }


        // Creates all roles that the bot needs and which have not been created manually yet.
        public async Task PopulateRoles()
        {
            if (ResidentGuild == null)
            {
                return;
            }

            // First add big quiet roles, then tiny quiet roles.
            foreach (string roleName in settings.BigQuietRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }

            foreach (string roleName in settings.TinyQuietRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }

            // In the ordering, then come big loud roles and tiny loud roles.
            foreach (string roleName in settings.BigLoudRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }

            foreach (string roleName in settings.TinyLoudRoles)
            {
                var sameNameRole = ResidentGuild.Roles.FirstOrDefault(x => x.Name == roleName);
                if (sameNameRole == null)
                {
                    await ResidentGuild.CreateRoleAsync(roleName, null, settings.roleColor(roleName));
                }
            }
        }

        // Call UpdateRank() only when you hold the mutex to the internal dictionaries.
        private async Task UpdateRank(Discord.WebSocket.SocketGuildUser player, Tuple<string, string> playerInfo)
        {
            // Ignore everything until ResidentGuild is set.
            if (ResidentGuild == null)
            {
                return;
            }

                try
                {
                    Tuple<int, int> rank = await GetCurrentRank(playerInfo.Item1, "EU", playerInfo.Item2);
                    Tuple<int, int> rankRoles = InferRankFromRoles(player);
                    if (rank.Item1 == -1)
                    {
                        // We were unsuccessful in parsing the rank for some reason, skip this player.
                        System.Console.WriteLine("We could not parse the rank of player " + player.Nickname + ".");
                    }

                if (rankRoles == null || rankRoles.Item1 != rank.Item1 || rankRoles.Item2 != rank.Item2)
                {
                    if (rankRoles != null)
                    {
                        System.Console.WriteLine("Inferred ranks " + rankRoles.Item1 + "," + rankRoles.Item2 + " -- fetched ranks " + rank.Item1 + "," + rank.Item2 + ".");
                    }
                    // We get reasonable information from the update, add new ranks to the player.
                    System.Console.WriteLine("Updating rank for player " + player.Nickname);

                    await ClearAllRanks(player);
                    await SetQuietRanks(ResidentGuild, player, rank);

                    if (!QuietPlayers.Contains(player.Id))
                    {
                        System.Console.WriteLine("The player is not quiet, we add the loud roles now.");
                        await AddLoudRoles(player, ResidentGuild);
                    }
                } else
                {
                    System.Console.WriteLine("Ranks match for player " + player.Nickname);
                }
            }
            catch (RankParsingException)
                {
                    System.Console.WriteLine("Failed to get rank for player " + player.Nickname);
                }
        }

        public async Task UpdateAll()
        {

            // Ignore everything until ResidentGuild is set
            if (ResidentGuild == null)
            {
                return;
            }

            await Access.WaitAsync();

            foreach (KeyValuePair<ulong, Tuple<string, string>> entry in DiscordUplay)
            {
                Discord.WebSocket.SocketGuildUser user = ResidentGuild.Users.FirstOrDefault(x => x.Id == entry.Key);

                if (user != null && !DoNotTrack.Contains(user.Id))
                {
                    await UpdateRank(user, entry.Value);
                }
                // TODO: Possibly erase from the DB if the user IS null.
            }
            Access.Release();
        }

        public async Task<Tuple<string, string>> QueryMapping(ulong discordId)
        {
            await Access.WaitAsync();

            Tuple<string, string> UplayNick = null;
            DiscordUplay.TryGetValue(discordId, out UplayNick);
            Access.Release();
            return UplayNick;
        }

        public async Task InsertIntoMapping(ulong discordId, string uplayNick, string platform)
        {
            await Access.WaitAsync();

            if (DoNotTrack.Contains(discordId))
            {
                Access.Release();
                throw new DoNotTrackException();
            }

            if (DiscordUplay.ContainsKey(discordId))
            {
                Access.Release();
                throw new DuplicateException();
            }

            Tuple<string, string> ins = new Tuple<string, string>(uplayNick, platform);
            DiscordUplay[discordId] = ins;

            Access.Release();
        }

        public async Task StopTracking(ulong discordId)
        {
            await Access.WaitAsync();

            if (DiscordUplay.ContainsKey(discordId))
            {
                DiscordUplay.Remove(discordId);
            }

            if (!DoNotTrack.Contains(discordId))
            {
                DoNotTrack.Add(discordId);
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
            while (true)
            {
                await UpdateAll();
                await BackupMappings();
                await Task.Delay(TimeSpan.FromSeconds(30));
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
            await new Bot().RunBotAsync();
        }
    }
}