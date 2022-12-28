﻿using System;
using System.Collections.ObjectModel;

namespace RankBot
{
    static class Settings
    {
        // ID of the guild (Discord server) that this instance operates on.
        // For the main use of this bot, this is the ID of Discord server R6 Siege a Chill, a CZ/SK Discord server.
        public static ulong residenceID = 620608384227606528;

        // ID of the guild where the channel with all internal data can be found.
        // If the bot is to be run on multiple Discord servers (guild), this should be a server where you have owner privileges.
        public static readonly ulong ControlGuild = 903649099541270528;
        public const string PrimaryConfigurationChannel = "primary-configuration";
        public static readonly string DataBackupChannel = "data-backups";
        // If the primary configuration channel is empty, the following file is read instead.
        public const string PrimaryConfigurationFile = @"primary.json";
        public const string backupFile = @"rsixbot.json";

        // Discord user IDs of all people that can run administrative commands through the bot.
        public static readonly ulong[] Operators = { 428263908281942038, 213681987561586693, 242905811175604225, 507961011815579649, 258974750422990848, 230336021219246081, 395934437910642688 };

        public static readonly TimeSpan updatePeriod = TimeSpan.FromHours(3); // How often do we update the ranks.
        public static readonly TimeSpan lockTimeout = TimeSpan.FromSeconds(1); // How long do we wait for acquiring the lock.

        public const string botStatus = "Pro info: /prikazy";

        // A global switch to turn off API synchronization, for example when the API changes.
        public static readonly bool ApiOfflineMode = true;

        // Enable or disable the logging mechanism.
        public static readonly bool Logging = true;

        // The bot needs to find at least the guild configuration file or message somewhere.
        // However, if you are running it for the first time, you may run it without a database of users.
        // In that case, set this variable to true, add at least one user, then call !backup to create the
        // data backup message, then set it back to false.
        public static readonly bool BotFirstRun = false;  

        public static readonly bool UsingExtensionMatchmaking = true;
        public static readonly bool UsingExtensionRoleHighlights = true;

        // This extension awards "Active Ranks" in addition to normal ranks.
        // The idea is to slightly reward activity on the guild by awarding an extra role.
        public static readonly bool UsingExtensionActiveUsers = true;

        /// <summary>
        /// Whether we should regenerate slash commands or not. Needs to be true for the first run
        /// and any run where you add or remove slash commands.
        /// </summary>
        public static readonly bool RegenerateSlashCommands = false;

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
