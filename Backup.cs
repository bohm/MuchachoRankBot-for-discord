using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace R6RankBot
{

    class BackupData
    {
        // The internal mapping between Discord names and R6TabIDs which we use to track ranks. Persists via backups.
        public Dictionary<ulong, string> discordUplayDict;
        // The internal data about ranks of Discord users. Does not persist on shutdown.
        public Dictionary<ulong, Rank> discordRanksDict;
        public HashSet<ulong> quietSet;
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