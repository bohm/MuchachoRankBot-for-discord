using System;

namespace R6RankBot
{
    class settings
    {
        // ID of the guild (Discord server) that this instance operates on.
        // For the main use of this bot, this is the ID of Discord server R6 Siege a Chill, a CZ/SK Discord server.
        public static ulong residenceID = 620608384227606528; 
        // ID of DoctorOrson, the current maintainer of this bot on the server.
        public static readonly ulong[] Operators = { 428263908281942038 };

        public static TimeSpan updatePeriod = TimeSpan.FromHours(3); // How often do we update the ranks.
        public static TimeSpan lockTimeout = TimeSpan.FromSeconds(1); // How long do we wait for acquiring the lock.

        private static string botStatus = "Napiste !prikazy pro informace.";

        public static readonly string[] BotChannels = { "rank-bot", "🦾rank-bot", "rank-bot-admin" }; // The only channels the bot is operating in.
        private static string logFolder = null;

        public static string backupFile = @"rsixbot.json";

        public static readonly string[] R6TabRanks = {
                            "unrank",
                            "Copper 4","Copper 3","Copper 2","Copper 1",
                            "Bronze 4", "Bronze 3", "Bronze 2", "Bronze 1",
                            "Silver 4", "Silver 3", "Silver 2", "Silver 1",
                            "Gold 4", "Gold 3", "Gold 2", "Gold 1",
                            "Platinum 3", "Platinum 2", "Platinum 1", "Diamond"}; // TODO: new ranks next season



        // Computes a colour based on the role type
        public static Discord.Color roleColor(string roleName)
        {
            // If it is a quiet role, we parse only by letters.
            if (roleName.Contains('U')) // Unranked.
            {
                return new Discord.Color(0x9f, 0x9f, 0x9f); // 9f9f9f
            }
            else if (roleName.Contains('C') && !(roleName.Contains('H') || roleName.Contains('h'))) // Copper.
            {
                return new Discord.Color(0xb8, 0x73, 0x33); // #b87333
            }
            else if (roleName.Contains('B')) // Bronze.
            {
                return new Discord.Color(0xcd, 0x7f, 0x32); // #cd7f32
            }
            else if (roleName.Contains('S')) // Silver.
            {
                return new Discord.Color(0xc0, 0xc0, 0xc0); // #c0c0c0
            }
            else if (roleName.Contains('G')) // Gold.
            {
                return new Discord.Color(0xff, 0xd7, 0x00); // #ffd700
            }
            else if (roleName.Contains('P')) // Platinum.
            {
                return new Discord.Color(0x00, 0xf9, 0xff); // #00f9ff
            } else if (roleName.Contains('D')) // Diamond.
            {
                return new Discord.Color(0xa4, 0x7d, 0xf4); // a47df4

            }
            else if (roleName.Contains('C') && (roleName.Contains('H') || roleName.Contains('h'))) // Champion.
            {
                return new Discord.Color(0xdd, 0x5d, 0xb0); // dd5db0
            }
            else // Unknown role, return "default" color. (non-nullable type)
            {
                return new Discord.Color();
            }
        }
        public static string get_botStatus()
        {
            return botStatus;
        }
        public static string get_logFolder()
        {
            return logFolder;
        }

        public static void set_botStatus(string txt)
        {
            botStatus = txt;
        }
        public static void set_logFolder(string txt)
        {
            logFolder = txt;
        }
    }
}
