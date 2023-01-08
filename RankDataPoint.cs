using System;
using System.Collections.Generic;
using System.Text;

namespace RankBot;

/// <summary>
/// One data point stored in the main rank database about a user.
/// It is essentially enough to store the MMR and the number of matches,
/// but we also represent the rank explicitly, not to convert it all the time.
/// </summary>
public record RankDataPointV6
{
    public bool playedAnyMatches = false;
    public int RankPoints = 0;

    public RankDataPointV6(bool playedMatches, int rankPoints)
    {
        RankPoints = rankPoints;
        playedAnyMatches = playedMatches;
    }

    public RankDataPointV6()
    {
    }

    public RankDataPointV6(UbisoftFullBoard fetchedBoard)
    {
        if (fetchedBoard.profile.rank != 0)
        {
            playedAnyMatches = true;
            RankPoints = fetchedBoard.profile.rank_points;
        }
    }

    public MetalV6 ToMetal()
    {
        if (!playedAnyMatches)
        {
            return MetalV6.Rankless;
        }

        return RankingV6.RankPointsToMetal(RankPoints);
    }

    public override string ToString()
    {
        return RankingV6.MetalPrint(ToMetal());
    }
}
