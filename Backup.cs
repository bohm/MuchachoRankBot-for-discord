using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RankBot
{
    public class SingleGuildConfig
    {
        public ulong id;
        public string reportChannel;
        public string loggingChannel; // Channel for logging data.
        public List<string> commandChannels;
        public List<string> roleHighlightChannels;
    }
    public class BackupGuildConfiguration
    {
        public List<SingleGuildConfig> guildList = new List<SingleGuildConfig>();

        public static BackupGuildConfiguration RestoreFromFile(string fileName)
        {
            BackupGuildConfiguration ret = null;
            if (File.Exists(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                StreamReader fileStream = File.OpenText(fileName);
                JsonTextReader file = new JsonTextReader(fileStream);
                ret = (BackupGuildConfiguration)serializer.Deserialize(file, typeof(BackupGuildConfiguration));
                file.Close();
            }

            return ret;
        }
        public static BackupGuildConfiguration RestoreFromString(string content)
        {
            BackupGuildConfiguration ret = null;
            TextReader stringr = new StringReader(content);
            JsonSerializer serializer = new JsonSerializer();
            ret = (BackupGuildConfiguration)serializer.Deserialize(stringr, typeof(BackupGuildConfiguration));
            return ret;
        }

        public void BackupToFile(string fileName)
        {

            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(fileName))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, this);
            }
        }

        public string BackupToString()
        {
            StringBuilder sb = new StringBuilder();
            JsonSerializer serializer = new JsonSerializer();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    serializer.Serialize(jw, this);
                }
            }

            return sb.ToString();
        }
    }

    class GuildConfigTest
    {
        public static void Run()
        {
            var gc = new BackupGuildConfiguration();

            var controlCenter = new SingleGuildConfig();
            controlCenter.id = Settings.ControlGuild;
            controlCenter.reportChannel = "reports";
            controlCenter.roleHighlightChannels = new List<string> {"looking-for-teammates"};
            controlCenter.commandChannels = new List<string> { "rank-bot" };

            var chillServer = new SingleGuildConfig();
            chillServer.id = 620608384227606528;
            chillServer.commandChannels = new List<string> { "🦾rank-bot", "rank-bot-admin" };
            chillServer.reportChannel = "🦾rank-bot";
            chillServer.roleHighlightChannels = new List<string> {"🔍hledám-spoluhráče"};

            gc.guildList.Add(controlCenter);
            gc.guildList.Add(chillServer);
            Console.WriteLine(gc.BackupToString());
        }
    }

    public class BackupData
    {
        // The internal mapping between Discord names and R6TabIDs which we use to track ranks.
        public Dictionary<ulong, string> discordUplayDict;
        // The internal data about ranks of Discord users.
        public Dictionary<ulong, RankDataPointV6> DiscordRanksV6;
        // public Dictionary<ulong, Rank> discordRanksDict;
        // public HashSet<ulong> quietSet;


        public static BackupData RestoreFromFile(string fileName)
        {
            BackupData ret = null;
            if (File.Exists(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                StreamReader fileStream = File.OpenText(fileName);
                JsonTextReader file = new JsonTextReader(fileStream);
                ret = (BackupData)serializer.Deserialize(file, typeof(BackupData));
                file.Close();
            }

            return ret;
        }
        public static BackupData RestoreFromString(string content)
        {
            BackupData ret = null;
            TextReader stringr = new StringReader(content);
            JsonSerializer serializer = new JsonSerializer();
            ret = (BackupData)serializer.Deserialize(stringr, typeof(BackupData));
            return ret;
        }

        public void BackupToFile(string fileName)
        {

            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(fileName))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, this);
            }
        }

        public string BackupToString()
        {
            StringBuilder sb = new StringBuilder();
            JsonSerializer serializer = new JsonSerializer();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    serializer.Serialize(jw, this);
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Methods handling serialization, local and remote backup of internal data structures, so that
    /// the bot can be restored quickly from any machine in case of a crash.
    /// </summary>
    class Backup
    { 
        public static BackupData RestoreFromFile(string fileName)
        {
            BackupData ret = null;
            if (File.Exists(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                StreamReader fileStream = File.OpenText(fileName);
                JsonTextReader file = new JsonTextReader(fileStream);
                ret = (BackupData)serializer.Deserialize(file, typeof(BackupData));
                file.Close();
            }

            return ret;
        }

        public static BackupData RestoreFromString(string content)
        {
            BackupData ret = null;
            TextReader stringr = new StringReader(content);
            JsonSerializer serializer = new JsonSerializer();
            ret = (BackupData)serializer.Deserialize(stringr, typeof(BackupData));
            return ret;
        }

        public static void BackupToFile(BackupData bd, string fileName)
        {

            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(fileName))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, bd);
            }
        }
    }

}