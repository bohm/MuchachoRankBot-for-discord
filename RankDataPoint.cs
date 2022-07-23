using System;
using System.Collections.Generic;
using System.Text;

namespace RankBot
{
    /// <summary>
    /// One data point stored in the main rank database about a user.
    /// It is essentially enough to store the MMR and the number of matches,
    /// but we also represent the rank explicitly, not to convert it all the time.
    /// </summary>
    class RankDataPoint
    {
        int MMR = 0;
        int matchesThisSeason = 0;
        Rank deducedRank;

        public RankDataPoint(int new_mmr, int matches)
        {
            MMR = new_mmr; matchesThisSeason = matches;
            deducedRank = SpecialRanks.Undefined;
            _ = DeduceRankData();
        }
        /// <summary>
        /// Computes the rank based on the internal mmr and played matches this season.
        /// <returns>True if deduced data changed, false if they stayed the same.</returns>
        /// </summary>
        public bool DeduceRankData()
        {
            Rank newrank = Ranking.RankComputation(MMR, matchesThisSeason);
            if (newrank != deducedRank)
            {
                deducedRank = newrank;
                return true;
            }
            return false;
        }
    }
}
