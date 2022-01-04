using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RankBot
{
    class DiscordGuilds
    {
        public List<DiscordGuild> guildList;
        public Dictionary<string, DiscordGuild> byName;
        public Dictionary<ulong, DiscordGuild> byID;

        public DiscordGuilds(BackupGuildConfiguration guildConfs, DiscordSocketClient sc)
        {
            guildList = new List<DiscordGuild>();
            byName = new Dictionary<string, DiscordGuild>();
            byID = new Dictionary<ulong, DiscordGuild>();
            foreach (var gc in guildConfs.guildList)
            {
                DiscordGuild guild = new DiscordGuild(gc, sc);
                guildList.Add(guild);
                byName.Add(guild.GetName(), guild);
                byID.Add(guild._socket.Id, guild);
                Console.WriteLine($"Connected to server {guild.GetName()}.");
            }
        }
    }


    class DiscordGuild
    {
        public SocketGuild _socket; // mild TODO: make this private in the future.
        private List<string> _reports;
        private readonly SemaphoreSlim _reports_lock;
        public SingleGuildConfig Config;


        public DiscordGuild(SingleGuildConfig gc, DiscordSocketClient sc)
        {
            _reports = new List<string>();
            _reports_lock = new SemaphoreSlim(1, 1);
            Config = gc;
            _socket = sc.GetGuild(Config.id);
        }

        public async Task RolePresenceCheckAsync()
        {
            bool roles_present = RoleCreation.CheckAllRoles(_socket);
            if (roles_present)
            {
                Console.WriteLine($"All roles present in {_socket.Name}.");
            }
            else
            {
                Console.WriteLine($"Some roles missing in {_socket.Name}, creating them.");
                await RoleCreation.CreateMissingRoles(_socket);
            }
        }

        public string GetName()
        {
            return _socket.Name;
        }

        public bool Ready()
        {
            return _socket != null;
        }

        /// <summary>
        /// Queries the Discord API to figure out if a user is a member of a guild.
        /// </summary>
        /// <param name="discordID"></param>
        /// <returns></returns>
        public bool IsGuildMember(ulong discordID)
        {
            if(_socket.Users.FirstOrDefault(x => x.Id == discordID) != null)
            {
                return true;
            }

            return false;
        }

        public async Task CleanSlate()
        {
            if (_socket == null)
            {
                return;
            }

            foreach (SocketGuildUser user in _socket.Users)
            {
                await RemoveAllRankRoles(user);
            }

            System.Console.WriteLine("The slate is now clean, no users have roles.");
        }

        /// <summary>
        /// Adds a string (a message from the bot to a public channel) to the list of pending messages.
        /// </summary>
        public void AddReport(string report)
        {
            _reports_lock.Wait();
            _reports.Add(report);
            _reports_lock.Release();
        }

        public async Task PublishReports()
        {
            await _reports_lock.WaitAsync();
            foreach (var report in _reports)
            {
                await Reply(report, Config.reportChannel);
            }

            _reports.Clear();
            _reports_lock.Release();
        }

        public async Task RemoveAllRankRoles(ulong discordID)
        {
            if (IsGuildMember(discordID))
            {
                SocketGuildUser u = GetSingleUser(discordID);
                await RemoveAllRankRoles(u);
            }
        }

        public async Task RemoveAllRankRoles(Discord.WebSocket.SocketGuildUser User)
        {
            System.Console.WriteLine("Removing all ranks from user " + User.Username);
            foreach (var RankRole in User.Roles.Where(x => Ranking.LoudMetalRoles.Contains(x.Name) || Ranking.LoudDigitRoles.Contains(x.Name)
                                                        || Ranking.SpectralMetalRoles.Contains(x.Name) || Ranking.SpectralDigitRoles.Contains(x.Name)))
            {
                await User.RemoveRoleAsync(RankRole);
            }
        }

        public async Task RemoveLoudRoles(Discord.WebSocket.SocketUser Author)
        {
            // If the user is in any mentionable rank roles, they will be removed.
            var Us = (Discord.WebSocket.SocketGuildUser)Author;
            // For each loud role, which can be a loud big role or a loud tiny role:
            foreach (var LoudRole in Us.Roles.Where(x => Ranking.LoudDigitRoles.Contains(x.Name) || Ranking.LoudMetalRoles.Contains(x.Name)))
            {
                await Us.RemoveRoleAsync(LoudRole);
            }
        }

        public SocketGuildUser GetSingleUser(ulong discordId)
        {
            return _socket.Users.FirstOrDefault(x => ((x.Id == discordId)));
        }
        public SocketGuildUser GetSingleUser(string discNameOrNick)
        {
            return _socket.Users.FirstOrDefault(x => ((x.Username == discNameOrNick) || (x.Nickname == discNameOrNick)));
        }

        public IEnumerable<SocketGuildUser> GetAllUsers(string discNameOrNick)
        {
            return _socket.Users.Where(x => ((x.Username == discNameOrNick) || (x.Nickname == discNameOrNick)));

        }

        public async Task ReplyToUser(string message, string channelName, ulong userID)
        {
            var replyChannel = _socket.TextChannels.FirstOrDefault(x => (x.Name == channelName));
            if (replyChannel == null)
            {
                return;
            }

            var user = this._socket.Users.FirstOrDefault(x => x.Id == userID);
            string fullMessage = $"{user.Username}: {message}";
            await replyChannel.SendMessageAsync(fullMessage);
        }

        public async Task Reply(string message, string channelName)
        {
            var replyChannel = _socket.TextChannels.FirstOrDefault(x => (x.Name == channelName));
            if (replyChannel == null)
            {
                return;
            }
            await replyChannel.SendMessageAsync(message);
        }

        public async Task Report(string message)
        {
            await Reply(message, Config.reportChannel);
        }

        public async Task AddLoudRoles(Discord.WebSocket.SocketUser Author, Rank rank)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add the non-digit role.
            string nonDigitLoudName = rank.CompactMetalPrint();
            var LoudRole = _socket.Roles.FirstOrDefault(x => x.Name == nonDigitLoudName);
            if (LoudRole != null)
            {
                await Us.AddRoleAsync(LoudRole);
            }

            // Then, if the rank has a digit role, add that too.
            if (rank.Digits())
            {
                string digitLoudName = rank.CompactFullPrint();
                var LoudDigitRole = _socket.Roles.FirstOrDefault(x => x.Name == digitLoudName);
                if (LoudDigitRole != null)
                {
                    await Us.AddRoleAsync(LoudDigitRole);
                }
            }
        }

        public async Task AddSpectralRoles(Discord.WebSocket.SocketUser Author, Rank rank)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add the non-digit role.
            string nonDigitSpectralName = rank.SpectralMetalPrint();
            var spectralRole = this._socket.Roles.FirstOrDefault(x => x.Name == nonDigitSpectralName);
            if (spectralRole != null)
            {
                await Us.AddRoleAsync(spectralRole);
            }

            // Then, if the rank has a digit role, add that too.
            if (rank.Digits())
            {
                string digitSpectralName = rank.SpectralFullPrint();
                var digitSpectralRole = _socket.Roles.FirstOrDefault(x => x.Name == digitSpectralName);
                if (digitSpectralRole != null)
                {
                    await Us.AddRoleAsync(digitSpectralRole);
                }
            }
        }
        public async Task UpdateRoles(ulong discordID, Rank newRank, bool userDoNotDisturb)
        {
            SocketGuildUser player = _socket.Users.FirstOrDefault(x => x.Id == discordID);
            if (player == null)
            {
                // The player probably left the Discord guild; we do not proceed.
                return;
            }

            await RemoveAllRankRoles(player);

            if (userDoNotDisturb)
            {
                Console.WriteLine("Updating spectral roles only for player " + player.Username);
                await AddSpectralRoles(player, newRank);
            }
            else
            {
                Console.WriteLine("Updating all roles only for player " + player.Username);
                await AddSpectralRoles(player, newRank);
                await AddLoudRoles(player, newRank);
            }
        }
    }

    class PrimaryDiscordGuild
    {
        public SocketGuild _socket; // soft TODO: make private
        public PrimaryDiscordGuild(DiscordSocketClient sc)
        {
            _socket = sc.GetGuild(Settings.ControlGuild);
            Console.WriteLine($"Connected to the primary Discord server {_socket.Name}.");
        }

        public async Task BackupFileToMessage(string dataFileLocation, string backupChannelName)
        {
            // Additionally, write the backup to Discord itself, so we can bootstrap from the Discord server itself and don't need any local files.
            SocketTextChannel backupChannel = _socket.TextChannels.SingleOrDefault(ch => ch.Name == backupChannelName);
            if (backupChannel != null)
            {
                // First, delete the previous backup. (This is why we also have a secondary backup.)
                var messages = backupChannel.GetMessagesAsync().Flatten();
                var msgarray = await messages.ToArrayAsync();
                if (msgarray.Count() > 1)
                {
                    Console.WriteLine($"The bot found {msgarray.Count()} messages but can only delete one due to safety. Aborting backup.");
                }

                if (msgarray.Count() == 1)
                {
                    await backupChannel.DeleteMessageAsync(msgarray[0]);
                }

                // Now, upload the new backup.
                await backupChannel.SendFileAsync(dataFileLocation, $"Backup file created at {DateTime.Now.ToShortTimeString()}.");
            }
        }
    }
}
