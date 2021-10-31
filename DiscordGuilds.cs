using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RankBot
{
    class DiscordGuilds
    {
        public List<DiscordGuild> guildList;
        public Dictionary<string, DiscordGuild> byName;
        public Dictionary<ulong, DiscordGuild> byID;

        public DiscordGuilds(List<BackupGuildConfiguration> guildConfs, DiscordSocketClient sc)
        {
            byName = new Dictionary<string, DiscordGuild>();
            byID = new Dictionary<ulong, DiscordGuild>();
            foreach (var gc in guildConfs)
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
        public BackupGuildConfiguration Config;
 
        public DiscordGuild(BackupGuildConfiguration gc, DiscordSocketClient sc)
        {
            Config = gc;
            _socket = sc.GetGuild(Config.id);
            _reports = new List<string>();
        }

        public string GetName()
        {
            return _socket.Name;
        }

        public bool Ready()
        {
            return _socket != null;
        }


        public async Task CleanSlate()
        {
            if (_socket == null)
            {
                return;
            }

            foreach (SocketGuildUser user in _socket.Users)
            {
                await dwrap.ClearAllRanks(user);
            }

            System.Console.WriteLine("The slate is now clean, no users have roles.");
            initComplete = true;
        }

        /// <summary>
        /// Adds a string (a message from the bot to a public channel) to the list of pending messages.
        /// </summary>
        public void AddReport(string report)
        {
            _reports.Add(report);
        }

        public async Task PublishReports()
        {
            var ReportChannel = _socket.TextChannels.FirstOrDefault(x => (x.Name == Settings.ReportChannel));

            if (ReportChannel == null)
            {
                return;
            }

            foreach (var report in _reports)
            {
                await ReportChannel.SendMessageAsync(report);
            }

            _reports.Clear();
        }
    }

    class PrimaryDiscordGuild
    {
        public SocketGuild _socket; // soft TODO: make private
        public PrimaryDiscordGuild(DiscordSocketClient sc)
        {
            _socket = sc.GetGuild(Settings.PrimaryServer);
            Console.WriteLine($"Connected to the primary Discord server {_socket.Name}.");
        }
    }
}
