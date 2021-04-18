using System;
using System.Collections.Generic;
using System.Text;

namespace R6RankBot
{
    /// <summary>
    /// One data point stored in the main rank database about a user.
    /// It is essentially enough to store the MMR and the number of matches,
    /// but we also represent the rank explicitly, not to convert it all the time.
    /// </summary>
    class RankDataPoint
    {
        int MMR = 0;
        // In principle, we would like to store matches this season, to compute the rank ourselves. However,
        // this is currently not easy to get from r6.tracker.network website. 
        // int matchesThisSeason = 0;
        bool enoughMatchesPlayed = false;
        Rank deducedRank;

        public RankDataPoint(int new_mmr, bool player_has_rank)
        {
            MMR = new_mmr; enoughMatchesPlayed = player_has_rank;
            DeduceRankData();
        }
        /// <summary>
        /// Computes the rank based on the internal mmr and played matches this season.
        /// <returns>True if deduced data changed, false if they stayed the same.</returns>
        /// </summary>
        public bool DeduceRankData()
        {
            Rank oldRank = deducedRank;

            if (!enoughMatchesPlayed)
            {
                deducedRank = new Rank(Metal.Rankless, 0);
            } else
            {
                deducedRank = Ranking.MMRToRank(MMR);
            }

            if (oldRank == null || !oldRank.Equals(deducedRank))
            {
                return true;
            }

            return false;
        }

        public bool UpdateDataPoint(int new_mmr, bool player_has_rank)
        {
            MMR = new_mmr; enoughMatchesPlayed = player_has_rank;
            return DeduceRankData();
        }
                
        public bool RankDeduced()
        {
            return (deducedRank != null);
        }
    }
}
