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
        // public Dictionary<ulong, Rank> DiscordRanks;
        // public HashSet<ulong> QuietPlayers;

        private Dictionary<ulong, RankDataPointV6> _discordRanks;

        // The semaphore (in fact a mutex) that blocks the access to DoNotTrack and DiscordUplay
        private readonly SemaphoreSlim Access;

        public BotDataStructure()
        {
            Access = new SemaphoreSlim(1, 1);
            DiscordUplay = new Dictionary<ulong, string>();
            // QuietPlayers = new HashSet<ulong>();
            _discordRanks = new Dictionary<ulong, RankDataPointV6>();
        }

        public BotDataStructure(BackupData backup)
        {
            Access = new SemaphoreSlim(1, 1);
            DiscordUplay = backup.discordUplayDict;
            // QuietPlayers = backup.quietSet;
            _discordRanks = backup.DiscordRanksV6;

            if (_discordRanks is null)
            {
                _discordRanks = new Dictionary<ulong, RankDataPointV6>();
            }
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
            // data.discordRanksDict = new Dictionary<ulong, Rank>(DiscordRanks);
            data.discordUplayDict = new Dictionary<ulong, string>(DiscordUplay);
            data.DiscordRanksV6 = new Dictionary<ulong, RankDataPointV6>(_discordRanks);
            // data.quietSet = new HashSet<ulong>(QuietPlayers);
            Access.Release();
            return data;
        }

        public async Task<bool> QueryQuietness(ulong discordUserID)
        {
            // Currently not implemented.
            return false;

            /* await Access.WaitAsync();
            bool ret = QuietPlayers.Contains(discordUserID);
            Access.Release();
            return ret; */
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

        public async Task<bool> RanksContainUser(ulong discordID)
        {
            await Access.WaitAsync();
            bool ret = _discordRanks.ContainsKey(discordID);
            Access.Release();
            return ret;
        }

        public async Task<RankDataPointV6> QueryRankInfo(ulong discordId)
        {
            await Access.WaitAsync();
            RankDataPointV6 query;
            _discordRanks.TryGetValue(discordId, out query);
            Access.Release();
            return query;
        }

        public async Task<MetalV6> QueryMetal(ulong discordId)
        {
            MetalV6 ret = MetalV6.Undefined;
            RankDataPointV6 query;
            await Access.WaitAsync();

            if (_discordRanks.TryGetValue(discordId, out query))
            {
                ret = query.ToMetal();
            }

            Access.Release();
            return ret;
        }


        public async Task RemoveFromDatabases(ulong discordId)
        {
            await Access.WaitAsync();

            if (DiscordUplay.ContainsKey(discordId))
            {
                DiscordUplay.Remove(discordId);
            }

            if (_discordRanks.ContainsKey(discordId))
            {
                _discordRanks.Remove(discordId);
            }

            /*
            if (QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Remove(discordId);
            }
            */

            Access.Release();
        }
        public async Task ShushPlayer(ulong discordId)
        {
            // Currently not implemented.
            return;
            /*
            await Access.WaitAsync();
            if (!QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Add(discordId);
            }
            Access.Release();
            */
        }

        public async Task MakePlayerLoud(ulong discordId)
        {
            // Currently not implemented.
            return;

            /*
            await Access.WaitAsync();
            if (QuietPlayers.Contains(discordId))
            {
                QuietPlayers.Remove(discordId);
            }
            Access.Release();
            */
        }

        internal async Task UpdateRanks(ulong discordID, RankDataPointV6 fetchedRank)
        {
            await Access.WaitAsync();
            _discordRanks[discordID] = fetchedRank;
            Access.Release();
        }

        internal async Task<Dictionary<ulong, string>> DuplicateUplayMapping()
        {
            await Access.WaitAsync();
            Dictionary<ulong, string> copy = new Dictionary<ulong, string>(DiscordUplay);
            Access.Release();
            return copy;
        }

        public int DiscordRankCount()
        {
            return _discordRanks.Count;
        }

        public int DiscordUplayCount()
        {
            return DiscordUplay.Count;
        }
    }
}
