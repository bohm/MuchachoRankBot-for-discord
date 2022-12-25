using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Extensions
{
    public class MmrDataStructure
    {
        public long LifetimeMMR;

        public MmrDataStructure(long lt)
        {
            LifetimeMMR = lt;
        }
    }

    public class MmrData
    {
        public Dictionary<ulong, MmrDataStructure> mmrs;
        public MmrData()
        {
            mmrs = new Dictionary<ulong, MmrDataStructure>();
        }
    }

    public class LifetimeMmr
    {
        PrimaryDiscordGuild pg;
        public MmrData Data;
        BackupSystem<MmrData> backup;
        public LifetimeMmr()
        {

        }
        public async Task DelayedInit(PrimaryDiscordGuild primg)
        {
            pg = primg;

            // Check if the primary discord guild contains the channel and if not, assume we run it for the first time.

            if (!pg._socket.TextChannels.Any(x => x.Name == "lifetime-mmr-tracking"))
            {
                Data = new MmrData();
                backup = await BackupSystem<MmrData>.NewBackupSystemAsync("lifetime-mmr-tracking", "lifetime-mmr-tracking.json", Data, pg);
                Console.WriteLine("Created a new backup system for lifetime MMR tracking.");
            }
            else
            {
                backup = new BackupSystem<MmrData>(pg, "lifetime-mmr-tracking", "lifetime-mmr-tracking.json");
                Data = await backup.RecoverAsync();
                Console.WriteLine($"Initialized {Data.mmrs.Count} elements from the lifetime MMR table backup.");
            }
        }

        public async Task SetMmr(ulong discordId, long lifetimeMmr)
        {
            Data.mmrs[discordId] = new MmrDataStructure(lifetimeMmr);
            await backup.BackupAsync(Data);

        }

        public long GetMmr(ulong discordId)
        {
            if (!Data.mmrs.ContainsKey(discordId))
            {
                return -1;
            } else
            {
                return Data.mmrs[discordId].LifetimeMMR;
            }
        }
    }
}
