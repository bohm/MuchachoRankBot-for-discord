﻿using System;

namespace R6RankBot
{
    static class Settings
    {
        // ID of the guild (Discord server) that this instance operates on.
        // For the main use of this bot, this is the ID of Discord server R6 Siege a Chill, a CZ/SK Discord server.
        public static ulong residenceID = 620608384227606528; 
        // ID of DoctorOrson, the current maintainer of this bot on the server.
        public static readonly ulong[] Operators = { 428263908281942038 };

        public static readonly TimeSpan updatePeriod = TimeSpan.FromHours(3); // How often do we update the ranks.
        public static readonly TimeSpan lockTimeout = TimeSpan.FromSeconds(1); // How long do we wait for acquiring the lock.

        public const string botStatus = "Napiste !prikazy pro informace.";

        public static readonly string[] BotChannels = { "rank-bot", "🦾rank-bot", "rank-bot-admin" }; // The only channels the bot is operating in.
        public const string backupFile = @"rsixbot.json";

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
    }
}
