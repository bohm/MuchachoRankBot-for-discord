using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RankBot
{
    /// <summary>
    /// A backup system that serializes a piece of data into JSON and posts it as a singular message on a Discord channel
    /// on the primary configuration server. Shared between this project and RoboModerator.
    /// </summary>
    /// <typeparam name="T">The class to be serialized and backed up.</typeparam>
    class BackupSystem<T>
    {
        private SemaphoreSlim _lock;
        private PrimaryDiscordGuild _guild;
        private string _channelName;
        private string _backupFileName;

        public BackupSystem(PrimaryDiscordGuild pg, string channelName, string backupFileName)
        {
            _guild = pg;
            _lock = new SemaphoreSlim(1, 1);
            _channelName = channelName;
            _backupFileName = backupFileName;
        }

        public async Task<T> RecoverAsync()
        {
            await _lock.WaitAsync();
            SocketTextChannel backupChannel = _guild._socket.TextChannels.FirstOrDefault(x => (x.Name == _channelName));

            if (backupChannel == null)
            {
                throw new BackupException($"We cannot find the channel named {_channelName}.");
            }


            var amsg = backupChannel.GetMessagesAsync();
            var messages = backupChannel.GetMessagesAsync().Flatten();
            var msgarray = await messages.ToArrayAsync();

            if (msgarray.Count() != 1)
            {
                throw new BackupException($"The bot found {msgarray.Count()} messages in channel {_channelName}" +
    "but only reads one due to safety. Aborting backup.");
            }

            var client = new HttpClient();
            var dataString = await client.GetStringAsync(msgarray[0].Attachments.First().Url);

            _lock.Release();

            return Deserialize(dataString);
        }

        public T Deserialize(string s)
        {
            T ret = default(T);
            TextReader stringr = new StringReader(s);
            JsonSerializer serializer = new JsonSerializer();
            ret = (T)serializer.Deserialize(stringr, typeof(T));
            return ret;
        }

        public async Task BackupAsync(T state, bool firstTimeBackup = false)
        {
            // First, backup to a file.

            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(_backupFileName))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, state);
            }

            await _lock.WaitAsync();

            // With the file ready, back up to the channel.
            // First, delete the previous backup. (This is why we also have a secondary backup.)
            SocketTextChannel backupChannel = _guild._socket.TextChannels.FirstOrDefault(x => (x.Name == _channelName));

            if (backupChannel == null)
            {
                throw new BackupException($"We cannot find the channel named {_channelName}.");
            }

            var messages = backupChannel.GetMessagesAsync().Flatten();
            var msgarray = await messages.ToArrayAsync();

            if (firstTimeBackup)
            {
                if (msgarray.Count() != 0)
                {
                    throw new BackupException($"The bot found {msgarray.Count()} messages in channel {_channelName}" +
                        "but there must be 0 for the initial backup.");
                }
            }
            else
            {
                if (msgarray.Count() > 1)
                {
                    throw new BackupException($"The bot found {msgarray.Count()} messages in channel {_channelName}" +
                        "but can only delete one due to safety. Aborting backup.");
                }

                if (msgarray.Count() == 1)
                {
                    await backupChannel.DeleteMessageAsync(msgarray[0]);
                }
            }

            // Now, upload the new backup.
            await backupChannel.SendFileAsync(_backupFileName, $"Backup for {state.GetType()} created at {DateTime.Now.ToShortTimeString()}.");
            _lock.Release();
        }

        public static async Task<BackupSystem<T>> NewBackupSystemAsync(string backupChannelName, string backupFileName, T initialBackup, PrimaryDiscordGuild pg)
        {
            if (pg._socket.TextChannels.Any(x => x.Name == backupChannelName))
            {
                throw new BackupException("A new backup system cannot be created with the same name!");
            }

            await pg._socket.CreateTextChannelAsync(backupChannelName);

            BackupSystem<T> bs = new BackupSystem<T>(pg, backupChannelName, backupFileName);
            await bs.BackupAsync(initialBackup, firstTimeBackup: true);
            return bs;
        }
    }
}