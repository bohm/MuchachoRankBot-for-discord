using RankBot.Commands.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RankBot;

public enum MetalV5: int
{
    Undefined = -2,
    Rankless = -1,
    Copper = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Diamond = 5,
    Champion = 6
}

public enum MetalV6: int
{
    Undefined = -1,
    Rankless = 0,
    Copper = 1,
    Bronze = 2,
    Silver = 3,
    Gold = 4,
    Platinum = 5,
    Emerald = 6,
    Diamond = 7,
    Champion = 8
}

public class RankingV6
{
    public static readonly string[] ColorMetalRoles =
    {
        "R", "C", "B", "S", "G", "P", "E", "D", "CH"
    };


    public static readonly int[] MetalLowThresholds =
    {
        0, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500
    };

    // An array corresponding to the low thresholds above.
    public static readonly MetalV6[] MetalRanks =
    {
        MetalV6.Rankless, MetalV6.Copper, MetalV6.Bronze, MetalV6.Silver, MetalV6.Gold, MetalV6.Platinum, MetalV6.Emerald, MetalV6.Diamond, MetalV6.Champion
    };

    public static MetalV6 RankPointsToMetal(int rankPoints)
    {
        int i = -1;
        while (i+1  < (MetalLowThresholds.Length) && rankPoints >= MetalLowThresholds[i+1])
        {
            i++;
        }

        if (i == -1)
        {
            return MetalV6.Undefined;
        }
        else
        {
            return MetalRanks[i];
        }
    }

    /// <summary>
    ///  Prints the full name of the metal. Use case: printing the user's rank.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static string MetalPrint(MetalV6 m)
    {
        switch (m)
        {
            case MetalV6.Rankless:
                return "Rankless";
            case MetalV6.Copper:
                return "Copper";
            case MetalV6.Bronze:
                return "Bronze";
            case MetalV6.Silver:
                return "Silver";
            case MetalV6.Gold:
                return "Gold";
            case MetalV6.Platinum:
                return "Platinum";
            case MetalV6.Emerald:
                return "Emerald";
            case MetalV6.Diamond:
                return "Diamond";
            case MetalV6.Champion:
                return "Champion";
            default:
                return "Undefined";
        }
    }

    public static Discord.Color MetalColor(MetalV6 m)
    {
        switch (m)
        {
            case MetalV6.Copper:
                return new Discord.Color(0xb8, 0x73, 0x33); // #b87333
            case MetalV6.Bronze:
                return new Discord.Color(0xcd, 0x7f, 0x32); // #cd7f32
            case MetalV6.Silver:
                return new Discord.Color(0xc0, 0xc0, 0xc0); // #c0c0c0
            case MetalV6.Gold:
                return new Discord.Color(0xff, 0xd7, 0x00); // #ffd700
            case MetalV6.Platinum:
                return new Discord.Color(0x00, 0xf9, 0xff); // #00f9ff
            case MetalV6.Emerald:
                return new Discord.Color(0x50, 0xce, 0x42); // #50ce42
            case MetalV6.Diamond:
                return new Discord.Color(0xa4, 0x7d, 0xf4); // a47df4
            case MetalV6.Champion:
                return new Discord.Color(0xdd, 0x5d, 0xb0); // dd5db0
            case MetalV6.Rankless:
                return new Discord.Color(0x9f, 0x9f, 0x9f); // 9f9f9f
            default:
                return new Discord.Color();
        }
    }
    /// <summary>
    /// Prints only the initial. We use that for the spectral
    /// roles -- the roles that only set colors and order in the 
    /// roster.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static string RolePrint(MetalV6 m)
    {
        if (m == MetalV6.Undefined)
        {
            throw new RankParsingException();
        } else
        {
            int index = (int)m;
            return ColorMetalRoles[index];
        }
    }
}


/// <summary>
/// Special ranks which are awarded based on number of games played.
/// </summary>
public struct SpecialRanks
{
    public static readonly Rank Undefined = new Rank(MetalV5.Undefined, 0);
    public static readonly Rank Rankless = new Rank(MetalV5.Rankless, 0);
    public static readonly Rank Champion = new Rank(MetalV5.Champion, 0);
}

/// <summary>
/// A static class holding parameters of ranking, Discord roles corresponding to the ranks
/// and converting metals.
/// </summary>
public class RankingV5
{
    public static readonly Rank[] NormalRanks =
    {
        new Rank(MetalV5.Copper, 5), new Rank(MetalV5.Copper, 4), new Rank(MetalV5.Copper, 3), new Rank(MetalV5.Copper, 2), new Rank(MetalV5.Copper, 1),
        new Rank(MetalV5.Bronze, 5), new Rank(MetalV5.Bronze, 4), new Rank(MetalV5.Bronze, 3), new Rank(MetalV5.Bronze, 2), new Rank(MetalV5.Bronze, 1),
        new Rank(MetalV5.Silver, 5), new Rank(MetalV5.Silver, 4), new Rank(MetalV5.Silver, 3), new Rank(MetalV5.Silver, 2), new Rank(MetalV5.Silver, 1),
        new Rank(MetalV5.Gold, 3), new Rank(MetalV5.Gold, 2), new Rank(MetalV5.Gold, 1),
        new Rank(MetalV5.Platinum, 3), new Rank(MetalV5.Platinum, 2), new Rank(MetalV5.Platinum, 1),
        new Rank(MetalV5.Diamond, 3), new Rank(MetalV5.Diamond, 2), new Rank(MetalV5.Diamond, 1),
    };

    /// <summary>
    /// Lower thresholds for ranks.
    /// Source: https://vignette.wikia.nocookie.net/rainbowsix/images/f/f3/Ranked_Skill_Levels.PNG/revision/latest?cb=20190913014632
    /// </summary>
    public static readonly int[] MMRLowThresholds =
    {
        // no limit for Unranked or Copper 5
        1200, 1300, 1400, 1500,
        1600, 1700, 1800, 1900, 2000,
        2100, 2200, 2300, 2400, 2500,
        2600, 2800, 3000,
        3200, 3500, 3800,
        4100, 4400, 4700,
        // 5000 -- We treat Champion rank differently, and so D1 is the highest rank you can get "without checking level".
    };


    public static readonly string[] LoudMetalRoles =
    {   
        "Rankless", "Copper", "Bronze", "Silver", "Gold", "Plat", "Dia", "Champ"
    };

    public static readonly string[] LoudDigitRoles =
    {
                        "Copper 5", "Copper 4","Copper 3","Copper 2", "Copper 1",
                        "Bronze 5", "Bronze 4", "Bronze 3", "Bronze 2", "Bronze 1",
                        "Silver 5", "Silver 4", "Silver 3", "Silver 2", "Silver 1",
                        "Gold 3", "Gold 2", "Gold 1",
                        "Plat 3", "Plat 2", "Plat 1",
                        "Dia 3", "Dia 2", "Dia 1"
    };

    public static readonly string[] SpectralMetalRoles =
    {
        "R", "C", "B", "S", "G", "P", "D", "CH"
    };

    public static readonly string[] SpectralDigitRoles =
{
                        "C5", "C4", "C3", "C2", "C1",
                        "B5", "B4", "B3", "B2", "B1",
                        "S5", "S4", "S3", "S2", "S1",
                        "G3", "G2", "G1",
                        "P3", "P2", "P1",
                        "D3", "D2", "D1"
    };

    /// <summary>
    /// Rank computation from MMR and matches played.
    /// Update when the ranking system changes.
    /// </summary>
    public static Rank RankComputation(int mmr, int matchesPlayedThisSeason)
    {
        if (matchesPlayedThisSeason < 10)
        {
            return SpecialRanks.Rankless;
        }

        if (matchesPlayedThisSeason >= 100 && mmr >= 5000)
        {
            return SpecialRanks.Champion;
        }

        else
        {
            return MMRToRank(mmr);
        }
    }

    public static Rank MMRToRank(int mmr)
    {
        int i = 0;
        while (i < (MMRLowThresholds.Length) && mmr >= MMRLowThresholds[i])
        {
            i++;
        }

        return NormalRanks[i];
    }

    /// <summary>
    ///  Prints the full name of the metal. Use case: printing the user's rank.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static string MetalPrint(MetalV5 m)
    {
        switch (m)
        {
            case MetalV5.Copper:
                return "Copper";
            case MetalV5.Bronze:
                return "Bronze";
            case MetalV5.Silver:
                return "Silver";
            case MetalV5.Gold:
                return "Gold";
            case MetalV5.Platinum:
                return "Platinum";
            case MetalV5.Diamond:
                return "Diamond";
            case MetalV5.Champion:
                return "Champion";
            case MetalV5.Rankless:
                return "Rankless";
            default:
                return "undefined";
        }
    }

    /// <summary>
    /// Transforms the metal name into a (Discord) color.
    /// Used for creating Discord roles.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static Discord.Color MetalColor(MetalV5 m)
    {
        switch (m)
        {
            case MetalV5.Copper:
                return new Discord.Color(0xb8, 0x73, 0x33); // #b87333
            case MetalV5.Bronze:
                return new Discord.Color(0xcd, 0x7f, 0x32); // #cd7f32
            case MetalV5.Silver:
                return new Discord.Color(0xc0, 0xc0, 0xc0); // #c0c0c0
            case MetalV5.Gold:
                return new Discord.Color(0xff, 0xd7, 0x00); // #ffd700
            case MetalV5.Platinum:
                return new Discord.Color(0x00, 0xf9, 0xff); // #00f9ff
            case MetalV5.Diamond:
                return new Discord.Color(0xa4, 0x7d, 0xf4); // a47df4
            case MetalV5.Champion:
                return new Discord.Color(0xdd, 0x5d, 0xb0); // dd5db0
            case MetalV5.Rankless:
                return new Discord.Color(0x9f, 0x9f, 0x9f); // 9f9f9f
            default:
                return new Discord.Color();
        }
    }
    /// <summary>
    /// Prints a compact name of the metal. We use that for the role names
    /// so that a user can just ping @Plat 3, and not necessarily write
    /// Platinum 3.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static string CompactPrint(MetalV5 m)
    {

        switch (m)
        {
            case MetalV5.Copper:
                return "Copper";
            case MetalV5.Bronze:
                return "Bronze";
            case MetalV5.Silver:
                return "Silver";
            case MetalV5.Gold:
                return "Gold";
            case MetalV5.Platinum:
                return "Plat";
            case MetalV5.Diamond:
                return "Dia";
            case MetalV5.Champion:
                return "Champ";
            case MetalV5.Rankless:
                return "Rankless";
            default:
                return "undefined";
        }
    }

    /// <summary>
    /// Prints only the initial. We use that for the spectral
    /// roles -- the roles that only set colors and order in the 
    /// roster.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static string InitialPrint(MetalV5 m)
    {
        switch (m)
        {
            case MetalV5.Copper:
                return "C";
            case MetalV5.Bronze:
                return "B";
            case MetalV5.Silver:
                return "S";
            case MetalV5.Gold:
                return "G";
            case MetalV5.Platinum:
                return "P";
            case MetalV5.Diamond:
                return "D";
            case MetalV5.Champion:
                return "CH";
            case MetalV5.Rankless:
                return "R";
            default:
                return "e";
        }
    }

    /// <summary>
    /// Returns a rank based on a string which corresponds to the spectral name of the role.
    /// Use sparingly, it is quite slow.
    /// </summary>
    /// <param name="rolename">The spectral role name which should be found.</param>
    /// <returns>Object of type rank that represents the found role, or Unranked, if it fails.</returns>
    public static Rank FindRankFromSpectral(string rolename)
    {
        foreach (Rank r in NormalRanks)
        {
            string spectralname = r.SpectralFullPrint();
            if (spectralname == rolename)
            {
                return r;
            }
        }
        // Try special ranks also.
        if (SpecialRanks.Rankless.SpectralFullPrint() == rolename)
        {
            return SpecialRanks.Rankless;
        }
        
        if (SpecialRanks.Champion.SpectralFullPrint() == rolename)
        {
            return SpecialRanks.Champion;
        }

        return SpecialRanks.Undefined;
    }
    /// <summary>
    /// When given a list of strings (presumably all roles of a user), returns only the spectral roles
    /// of that user. Can be combined with FindRankFromSpectral() to actually get a rank from a list
    /// of roles.
    /// </summary>
    /// <param name="UserRoles">A list of roles which you want to be filtered.</param>
    /// <returns>Spectral roles present in the list.</returns>
    public static List<string> FilterSpectralRoles(List<string> UserRoles)
    {
        List<string> allSpectralRoles = SpectralMetalRoles.Concat(SpectralDigitRoles).ToList();
        return UserRoles.Where(ur => allSpectralRoles.Contains(ur)).ToList();
    }

    public static Rank GuessRank(List<string> UserRoles)
    {
        Rank ret = new Rank(MetalV5.Undefined, 0);
        List<string> specRoles = FilterSpectralRoles(UserRoles);
        foreach (string specRole in specRoles)
        {
            Rank guess = FindRankFromSpectral(specRole);
            if (!guess.Equals(ret))
            {
                ret = guess;
                break;
            }
        }
        return ret;
    }
}

public class Rank
{
    public MetalV5 met;
    public int level = 0;

    public Rank(MetalV5 m, int l)
    {
        met = m; level = l;
    }

    public bool Equals(Rank other)
    {
        return (met == other.met && level == other.level);
    }

    /// <summary>
    /// Return whether the given rank actually has a digit associated with it,
    /// such as Copper 4, as opposed to Champion.
    /// </summary>
    /// <returns></returns>
    public bool Digits()
    {
        if (met == MetalV5.Undefined || met == MetalV5.Rankless || met == MetalV5.Champion)
        {
            return false;
        }
        return true;
    }
    public string FullPrint()
    {
        string output = RankingV5.MetalPrint(met);
        if (Digits())
        {
            output = output + " " + level;
        }
        return output;
    }

    public override string ToString() => FullPrint();
    public string CompactMetalPrint()
    {
        return RankingV5.CompactPrint(met);
    }
    public string CompactFullPrint()
    {
        string output = RankingV5.CompactPrint(met);
        if (Digits())
        {
            output = output + " " + level;
        }
        return output;
    }

    public string SpectralMetalPrint()
    {
        return RankingV5.InitialPrint(met);
    }

    public string SpectralFullPrint()
    {
        string output = RankingV5.InitialPrint(met);
        if (Digits())
        {
            // No blank space in this case.
            output += level;
        }
        return output;
    }

    /// <summary>
    /// Returns a list of 1-4 roles which the user with this role should be added to.
    /// Depends on whether the role has a digit (like Silver 3) or not (like Diamond).
    /// </summary>
    /// <param name="loud">Whether to add loud roles or not.</param>
    /// <returns></returns>
    public List<string> Rolenames(bool loud = true)
    {
        List<string> ret = new List<string>();

        // Spectral roles
        ret.Add(SpectralMetalPrint());
        if (Digits())
        {
            ret.Add(SpectralFullPrint());
        }

        // Loud roles
        if (loud)
        {
            ret.Add(CompactMetalPrint());
            if (Digits())
            {
                ret.Add(CompactFullPrint());
            }
        }
        return ret;
    }

}
