using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;

namespace DiscordBot
{
    public class RankParsingException : Exception
    {
        public RankParsingException()
        {
        }

        public RankParsingException(string message)
            : base(message)
        {
        }

        public RankParsingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    class Bot
    {
        static void Main(string[] args) => new Bot().RunBotAsync().GetAwaiter().GetResult();
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;

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
            foreach (var RankRole in User.Roles.Where(x => settings.LoudBigRoles.Contains(x.Name) || settings.LoudTinyRoles.Contains(x.Name) || settings.QuietBigRoles.Contains(x.Name) || settings.QuietTinyRoles.Contains(x.Name)))
            {
                await User.RemoveRoleAsync(RankRole);
            }
        }
        public static async Task RemoveLoudRoles(Discord.WebSocket.SocketUser Author, Discord.WebSocket.SocketGuild Guild)
        {
            // If the user is in any mentionable rank roles, they will be removed.
            var Us = (Discord.WebSocket.SocketGuildUser) Author;
            // For each loud role, which can be a loud big role or a loud tiny role:
            foreach (var LoudRole in Us.Roles.Where(x => settings.LoudBigRoles.Contains(x.Name) || settings.LoudTinyRoles.Contains(x.Name) ) ) 
            {
                await Us.RemoveRoleAsync(LoudRole);
            }
        }

        public static async Task AddLoudRoles(Discord.WebSocket.SocketUser Author, Discord.WebSocket.SocketGuild Guild)
        {
            var Us = (Discord.WebSocket.SocketGuildUser) Author;
            
            // First add big roles;
            foreach (var QuietRole in Us.Roles.Where(x => settings.QuietBigRoles.Contains(x.Name)))
            {
                int index = RoleNameIndex(QuietRole.Name, settings.QuietBigRoles);
                if (index != -1)
                {
                    string Corresponding = settings.LoudBigRoles[index];
                    var LoudRole = Guild.Roles.FirstOrDefault(x => x.Name == Corresponding);
                    if (LoudRole != null)
                    {
                        System.Console.WriteLine("Adding role " + LoudRole.Name);
                        await Us.AddRoleAsync(LoudRole);
                    }
                }
            }

            // Then add tiny roles.
            foreach (var QuietRole in Us.Roles.Where(x => settings.QuietTinyRoles.Contains(x.Name)))
            {
                int index = RoleNameIndex(QuietRole.Name, settings.QuietTinyRoles);
                if (index != -1)
                {
                    string Corresponding = settings.LoudTinyRoles[index];
                    var LoudRole = Guild.Roles.FirstOrDefault(x => x.Name == Corresponding);
                    if (LoudRole != null)
                    {
                        System.Console.WriteLine("Adding role " + LoudRole.Name);
                        await Us.AddRoleAsync(LoudRole);
                    }
                }
            }
        }

        // TODO: Implement platform.
        public static async Task<Tuple<int, int> > GetCurrentRank(string Nick, string Region, string Platform)
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

        public async Task RunBotAsync()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
            string botToken = "NjE5OTI1MjA4NTEzNjQyNTM3.XXPU0A.8V0GeMdg7u7nGX-ipnxqXQoWR-g";
            // string botToken = "TOKEN";
            client.Log += Log;
            await RegisterCommandsAsync();
            await client.LoginAsync(Discord.TokenType.Bot, botToken);
            await client.StartAsync();
            await client.SetGameAsync(settings.get_botStatus());
            await Task.Delay(-1);
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
    }
}
