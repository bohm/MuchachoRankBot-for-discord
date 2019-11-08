namespace DiscordBot
{
    class settings
    {
        // IDs of: DoctorOrson, GameOver.
        public static readonly ulong[] Operators = { 428263908281942038, 213681987561586693 };
        private static string botStatus = "Napiste !prikazy pro informace.";

        public static readonly string[] BotChannels = { "rank-bot", "rank-bot-admin" }; // The only channels the bot is operating in.
        private static string logFolder = null;

        public static string serializeFile = @"rsix.json";

        public static readonly string[] R6TabRanks = {
                            "unrank",
                            "Copper 4","Copper 3","Copper 2","Copper 1",
                            "Bronze 4", "Bronze 3", "Bronze 2", "Bronze 1",
                            "Silver 4", "Silver 3", "Silver 2", "Silver 1",
                            "Gold 4", "Gold 3", "Gold 2", "Gold 1",
                            "Platinum 3", "Platinum 2", "Platinum 1", "Diamond"}; // TODO: new ranks next season + champion

        public static readonly string[] BigLoudRoles =
{
            "Unrank", "Copper", "Bronze", "Silver", "Gold", "Plat", "Dia", "Champ"
        };

        public static readonly string[] TinyLoudRoles =
        {
                            "Copper 4","Copper 3","Copper 2","Copper 1",
                            "Bronze 4", "Bronze 3", "Bronze 2", "Bronze 1",
                            "Silver 4", "Silver 3", "Silver 2", "Silver 1",
                            "Gold 4", "Gold 3", "Gold 2", "Gold 1",
                            "Plat 3", "Plat 2", "Plat 1",
        };

        public static readonly string[] BigQuietRoles =
        {
            "U", "C", "B", "S", "G", "P", "D", "CH"
        };

        public static readonly string[] TinyQuietRoles =
{
                            "C4","C3","C2","C1",
                            "B4", "B3", "B2", "B1",
                            "S4", "S3", "S2", "S1",
                            "G4", "G3", "G2", "G1",
                            "P3", "P2", "P1",
        };

        public static readonly string ChillRole = "Full Chill";

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
        // Gives you a big role array index from the R6Tab rank array index.
        // You may want to change this if the R6Tab API changes.

        public static int BigRoleFromRank(int TabRank)
        {
            if (TabRank == 0)
            {
                return 0; // Unranked or U.
            }

            if (TabRank >= 1 && TabRank <= 4)
            {
                return 1; // Copper or C.
            }

            if (TabRank >= 5 && TabRank <= 8)
            {
                return 2; // Bronze or B.
            }

            if (TabRank >= 9 && TabRank <= 12)
            {
                return 3; // Silver or S.
            }

            if (TabRank >= 13 && TabRank <= 16)
            {
                return 4; // Gold or G.
            }

            if (TabRank >= 17 && TabRank <= 19)
            {
                return 5; // Plat or P.
            }

            else // (TabRank == 20)
            {
                return 6; // Dia or D.
            }
        }

        // Gives you a tiny role array index from the R6Tab rank array index.
        // You may want to change this if the R6Tab API changes.
        public static int TinyRoleFromRank(int TabRank)
        {
            if (TabRank == 0 || TabRank == 20)
            {
                // Unranked and Diamond do not have tiny roles.
                return -1;
            } else
            {
                return TabRank - 1; // You can see above that the TinyRole array corresponds to the R6 Tab array.
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
