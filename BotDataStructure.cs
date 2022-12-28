using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RankBot
{
    public class BotDataStructure
    {
        // The internal mapping between Discord names and R6TabIDs which we use to track ranks. 
        public Dictionary<ulong, string> DiscordUplay;
        // The internal data about ranks of Discord users.
        public Dictionary<ulong, Rank> DiscordRanks;
        public HashSet<ulong> QuietPlayers;

        // The semaphore (in fact a mutex) that blocks the access to DoNotTrack and DiscordUplay
        private readonly SemaphoreSlim Access;

        public BotDataStructure()
        {
            Access = new SemaphoreSlim(1, 1);
            DiscordUplay = new Dictionary<ulong, string>();
            QuietPlayers = new HashSet<ulong>();
            DiscordRanks = new Dictionary<ulong, Rank>();
        }

        public BotDataStructure(BackupData backup)
        {
            Access = new SemaphoreSlim(1, 1);
            DiscordUplay = backup.discordUplayDict;
            QuietPlayers = backup.quietSet;
            DiscordRanks = backup.discordRanksDict;
        }

        /// <summary>
        /// Creates a copy of the current data structures for backup.
        /// Extensions need to be handled separately.
        /// </summary>
        /// <returns></returns>
        public async Task<BackupData> PrepareBackup()
        {
            await Access.WaitAsync();
            BackupData data = new BackupData();
            data.discordRanksDict = new Dictionary<ulong, Rank>(DiscordRanks);
            data.discordUplayDict = new Dictionary<ulong, string>(DiscordUplay);
            data.quietSet = new HashSet<ulong>(QuietPlayers);
            Access.Release();
            return data;
        }

        public async Task<bool> QueryQuietness(ulong discordUserID)
        {
            await Access.WaitAsync();
            bool ret = QuietPlayers.Contains(discordUserID);
            Access.Release();
            return ret;
        }


        public async Task<bool> UserTracked(ulong discordID)
        {
            await Access.WaitAsync();
            bool contains = DiscordUplay.ContainsKey(discordID);
            Access.Release();
            return contains;
        }

        public async Task<string> QueryMapping(ulong discordId)
        {
            await Access.WaitAsync();

            string r6TabId = null;
            DiscordUplay.TryGetValue(discordId, out r6TabId);
            Access.Release();
            return r6TabId;
        }

        public async Task InsertIntoMapping(ulong discordId, string r6TabId)
        {
            await Access.WaitAsync();

            if (DiscordUplay.ContainsKey(discordId))
            {
                Access.Release();
                throw new DuplicateException();
            }

            DiscordUplay[discordId] = r6TabId;
            Access.Release();
        }

        public async Task<bool> TrackingContains(ulong discordID)
        {
            await Access.WaitAsync();
            bool ret = DiscordRanks.ContainsKey(discordID);
            Access.Release();
            return ret;
        }

        public async Task<Rank> QueryRank(ulong DiscordID)
        {
            await Access.WaitAsync();
            Rank r;
            DiscordRanks.TryGetValue(DiscordID, out r);
            Access.Release();
            return r;
        }
        /// <summary>
        /// Inserts into the DiscordRanks internal dictionary of mapping from
        /// ids to ranks. Only call when having access to the database.
        /// </summary>
        /// <param name="discordID"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public async Task InsertIntoRanks(ulong discordID, Rank r)
        {
            // await Access.WaitAsync();
            DiscordRanks[discordID] = r;
            // Access.Release();
        }

        public async Task RemoveFromDatabases(ulong discordId)
        {
            await Access.WaitAsync();

            if (DiscordUplay.ContainsKey(discordId))
            {
                DiscordUplay.Remove(discordId);
            }

            if (DiscordRanks.ContainsKey(discordId))
            {
                DiscordRanks.Remove(discordId);
            }

            if (QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Remove(discordId);
            }

            Access.Release();
        }
        public async Task ShushPlayer(ulong discordId)
        {
            await Access.WaitAsync();
            if (!QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Add(discordId);
            }
            Access.Release();
        }

        public async Task MakePlayerLoud(ulong discordId)
        {
            await Access.WaitAsync();
            if (QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Remove(discordId);
            }
            Access.Release();
        }

        internal async Task UpdateRanks(ulong discordID, Rank fetchedRank)
        {
            await Access.WaitAsync();
            DiscordRanks[discordID] = fetchedRank;
            Access.Release();
        }

        internal async Task<Dictionary<ulong, string>> DuplicateUplayMapping()
        {
            await Access.WaitAsync();
            Dictionary<ulong, string> copy = new Dictionary<ulong, string>(DiscordUplay);
            Access.Release();
            return copy;
        }
    }
}
