using RankBot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RankBot.Extensions
{
    class BanData
    {
        public string OriginalUplayName;
        public string UplayId;
        public bool Banned;
        public int BanType;
        public ulong ReporterId;
        public ulong GuildWhereAsked; // ID of the Discord Guild where a report was made.

        public BanData(string uplayName, string uplayId, ulong reporter, ulong whereAsked)
        {
            OriginalUplayName = uplayName;
            UplayId = uplayId;
            Banned = false;
            BanType = 0;
            ReporterId = reporter;
            GuildWhereAsked = whereAsked;
        }
    }

    class BanDataStructure
    {
        public Dictionary<string, string> OrigNickToUplay;
        public Dictionary<string, BanData> BanDict;

        public BanDataStructure()
        {
            OrigNickToUplay = new Dictionary<string, string>();
            BanDict = new Dictionary<string, BanData>();
        }
    }
    class BanTracking
    {
        private DiscordGuilds _guilds;
        // private DiscordWrapper _dw;
        public SemaphoreSlim BanTreeAccess;
        private StatsDB statsdb;
        private BanDataStructure bds;
        private int NumberOfQueries = 0;
        public BanTracking(DiscordGuilds g, BanDataStructure savedDS)
        {
            _guilds = g;
            statsdb = new StatsDB();
            BanTreeAccess = new SemaphoreSlim(1,1);
            bds = savedDS;
            Console.WriteLine($"Loaded {bds.BanDict.Count} ban dict elements from the last backup");
        }

        public BanTracking(DiscordGuilds g)
        {
            Console.WriteLine("Creating bantracking without any previous data.");
            _guilds = g;
            statsdb = new StatsDB();
            BanTreeAccess = new SemaphoreSlim(1, 1);
            bds = new BanDataStructure();
        }

        public BanDataStructure DuplicateData()
        {
            BanTreeAccess.Wait();
            BanDataStructure ret = new BanDataStructure();
            ret.BanDict = new Dictionary<string, BanData>(bds.BanDict);
            ret.OrigNickToUplay = new Dictionary<string, string>(bds.OrigNickToUplay);
            BanTreeAccess.Release();
            return ret;
        }

        public (bool, bool) QueryBanByNick(string oldNickname)
        {
            bool found = false, ban = false;
            BanTreeAccess.Wait();
            if (bds.OrigNickToUplay.ContainsKey(oldNickname))
            {
                string uplayId = bds.OrigNickToUplay[oldNickname];
                if (!bds.BanDict.ContainsKey(uplayId))
                {
                    // Internal consistency error.
                    BanTreeAccess.Release();
                    throw new Exception();
                }
                else
                {
                    found = true;
                    ban =  bds.BanDict[uplayId].Banned;
                }
            }
 
            BanTreeAccess.Release();
            return (found, ban);
        }

        public BanData QueryBanData(string oldNickname)
        {
            BanData ret = null;
            BanTreeAccess.Wait();
            if (bds.OrigNickToUplay.ContainsKey(oldNickname))
            {
                string uplayId = bds.OrigNickToUplay[oldNickname];
                if (!bds.BanDict.ContainsKey(uplayId))
                {
                    // Internal consistency error.
                    BanTreeAccess.Release();
                    throw new Exception();
                }
                else
                {
                    ret = bds.BanDict[uplayId];
                }
            }
            BanTreeAccess.Release();
            return ret;
        }

        public (bool, int) QueryBanTracker(string uplayId)
        {
            NumberOfQueries++;
            return statsdb.CheckUserBan(uplayId);
        }



        private async Task SuspectFirstCheck(string uplayId)
        {

            BanTreeAccess.Wait();

            // Actually only one report, but we still use the List<> function to provide the report.
            List<string> newlyBannedReports = new List<string>();
            // Note: we only check for people that have not been banned yet.
            // This is possibly undesirable, but it reduces the number of calls to the tracker.
            if ( bds.BanDict.ContainsKey(uplayId) && bds.BanDict[uplayId].Banned )
            {
                // Do not return, we still hold the lock.
            } else
            {
                bool banned = false;
                int banType = 0;
                try
                {
                    (banned, banType) = QueryBanTracker(uplayId);
                }
                catch (BanParsingException e)
                {
                    Console.WriteLine($"{String.Format("{0:r}", DateTime.Now)}: Encountered errors, cannot check the user for now.");
                }
                if (!bds.BanDict.ContainsKey(uplayId))
                {
                    // Internal consistency failure.
                    BanTreeAccess.Release();
                    throw new Exception();
                }
                else
                {
                    bds.BanDict[uplayId].Banned = banned;
                    if (banned)
                    {
                        bds.BanDict[uplayId].BanType = banType;
                        string report = BuildBanReport(bds.BanDict[uplayId]);
                        _guilds.byID[bds.BanDict[uplayId].GuildWhereAsked].AddReport(report);
                    }
                }
            }
            BanTreeAccess.Release();
        }

        public async Task InsertSuspect(string uplayId, string originalNick, ulong reporterID, ulong discordWhereAsked)
        {
            BanTreeAccess.Wait();
            BanData bd = new BanData(originalNick, uplayId, reporterID, discordWhereAsked);
            bds.OrigNickToUplay.Add(originalNick, uplayId);
            bds.BanDict.Add(uplayId, bd);
            BanTreeAccess.Release();
            await SuspectFirstCheck(uplayId);
        }   

        public void DeleteSuspectByNick(string originalNick)
        {

            BanTreeAccess.Wait();
            if (bds.OrigNickToUplay.ContainsKey(originalNick))
            {
                string uplayId = bds.OrigNickToUplay[originalNick];
                bds.OrigNickToUplay.Remove(originalNick);
                if (bds.BanDict.ContainsKey(uplayId))
                {
                    bds.BanDict.Remove(uplayId);
                }
            }
            BanTreeAccess.Release();
        }
        
        public void DeleteSuspectById(string uplayId)
        {
            BanTreeAccess.Wait();
            if (bds.BanDict.ContainsKey(uplayId))
            {
                string originalNick = bds.BanDict[uplayId].OriginalUplayName;
                bds.BanDict.Remove(uplayId);
                if (bds.OrigNickToUplay.ContainsKey(originalNick))
                {
                    bds.OrigNickToUplay.Remove(originalNick);
                }
            }
            BanTreeAccess.Release();
        }

        public string BuildBanReport(BanData newlyBannedUser)
        {
            string statsDBUrl = StatsDB.BuildURL(newlyBannedUser.OriginalUplayName, newlyBannedUser.UplayId);
            return $"Podezrely {newlyBannedUser.OriginalUplayName} dostal ban. Clen Discordu <@{newlyBannedUser.ReporterId}> mel pravdu! Cheateruv profil na StatsDB: {statsDBUrl} .";
        }

        public async void ExtendBackup(BackupData data)
        {
            data.bds = DuplicateData();
        }
        public async void UpdateStructure(object _)
        {
            // Requests and releases the lock for us.
            List<Tuple<ulong, string>> newlyBannedReports = new List<Tuple<ulong,string>>();
            BanDataStructure localBds = DuplicateData();
            foreach ((string uplayId, BanData data) in localBds.BanDict)
            {
                if (!data.Banned)
                {
                    bool newban = false;
                    int newreason = 0;
                    try
                    {
                        (newban, newreason) = QueryBanTracker(uplayId);
                    } catch (BanParsingException e)
                    {
                        Console.WriteLine($"{String.Format("{0:r}", DateTime.Now)}: Encountered errors, aborting updating the ban structure for now.");
                        return;
                    }
                    if (newban)
                    {
                        await BanTreeAccess.WaitAsync();
                        // This is almost always not necessary, but it can happen that a user is deleted from the database
                        // between the duplication and this update step.
                        if (bds.BanDict.ContainsKey(uplayId))
                        {
                            bds.BanDict[uplayId].Banned = true;
                            bds.BanDict[uplayId].BanType = newreason;

                            // Create a ban report.
                            newlyBannedReports.Add(new Tuple<ulong,string>(bds.BanDict[uplayId].GuildWhereAsked, BuildBanReport(bds.BanDict[uplayId])));
                        }
                        BanTreeAccess.Release();
                    }
                }
            }

            if (newlyBannedReports.Count >= 1)
            {
                foreach ((ulong guildID, string report) in newlyBannedReports)
                {
                    // A transitional correction.
                    if (guildID == 0)
                    {
                        _guilds.byID[Settings.ControlGuild].AddReport(report);
                    }
                    else
                    {
                        if (!_guilds.byID.ContainsKey(guildID))
                        {
                            throw new GuildListException($"The guild with ID {guildID} is not currently being serviced.");
                        }
                        _guilds.byID[guildID].AddReport(report);
                    }
                }
            }

            // Post the reports.
            foreach (DiscordGuild dg in _guilds.byID.Values)
            {
                await dg.PublishReports();
            }
            Console.WriteLine($"Performed {NumberOfQueries} queries from the last update structure task to the present.");
            NumberOfQueries = 0;
        }
    }
}
