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
    class BotDataStructure
    {
        // The internal mapping between Discord names and R6TabIDs which we use to track ranks. 
        public Dictionary<ulong, string> DiscordUplay;
        // The internal data about ranks of Discord users.
        public Dictionary<ulong, Rank> DiscordRanks;
        public HashSet<ulong> QuietPlayers;

        // The semaphore (in fact a mutex) that blocks the access to DoNotTrack and DiscordUplay
        private readonly SemaphoreSlim Access;

        // Extension data structures.
        private Extensions.RoleHighlighting _rh;
        public Extensions.BanTracking bt;

        public BotDataStructure()
        {
            Access = new SemaphoreSlim(1, 1);
            if (Settings.UsingExtensionRoleHighlights)
            {
                _rh = new Extensions.RoleHighlighting(dwrap);
            }

            if (Settings.UsingExtensionBanTracking)
            {
                bt = new Extensions.BanTracking(dwrap);
            }
        }


        public async Task<BackupData> PrepareBackup()
        {
            await Access.WaitAsync();
            BackupData data = new BackupData();
            data.discordRanksDict = new Dictionary<ulong, Rank>(DiscordRanks);
            data.discordUplayDict = new Dictionary<ulong, string>(DiscordUplay);
            data.quietSet = new HashSet<ulong>(QuietPlayers);
            Access.Release();

            if (Settings.UsingExtensionBanTracking)
            {
                data.bds = bt.DuplicateData();
            }

            return data;
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
    }
}
