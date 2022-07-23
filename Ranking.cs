﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RankBot
{
    public enum Metal: int
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

    /// <summary>
    /// Special ranks which are awarded based on number of games played.
    /// </summary>
    public struct SpecialRanks
    {
        public static readonly Rank Undefined = new Rank(Metal.Undefined, 0);
        public static readonly Rank Rankless = new Rank(Metal.Rankless, 0);
        public static readonly Rank Champion = new Rank(Metal.Champion, 0);
    }

    /// <summary>
    /// A static class holding parameters of ranking, Discord roles corresponding to the ranks
    /// and converting metals.
    /// </summary>
    public class Ranking
    {
        public static readonly Rank[] NormalRanks =
        {
            new Rank(Metal.Copper, 5), new Rank(Metal.Copper, 4), new Rank(Metal.Copper, 3), new Rank(Metal.Copper, 2), new Rank(Metal.Copper, 1),
            new Rank(Metal.Bronze, 5), new Rank(Metal.Bronze, 4), new Rank(Metal.Bronze, 3), new Rank(Metal.Bronze, 2), new Rank(Metal.Bronze, 1),
            new Rank(Metal.Silver, 5), new Rank(Metal.Silver, 4), new Rank(Metal.Silver, 3), new Rank(Metal.Silver, 2), new Rank(Metal.Silver, 1),
            new Rank(Metal.Gold, 3), new Rank(Metal.Gold, 2), new Rank(Metal.Gold, 1),
            new Rank(Metal.Platinum, 3), new Rank(Metal.Platinum, 2), new Rank(Metal.Platinum, 1),
            new Rank(Metal.Diamond, 3), new Rank(Metal.Diamond, 2), new Rank(Metal.Diamond, 1),
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
        public static string MetalPrint(Metal m)
        {
            switch (m)
            {
                case Metal.Copper:
                    return "Copper";
                case Metal.Bronze:
                    return "Bronze";
                case Metal.Silver:
                    return "Silver";
                case Metal.Gold:
                    return "Gold";
                case Metal.Platinum:
                    return "Platinum";
                case Metal.Diamond:
                    return "Diamond";
                case Metal.Champion:
                    return "Champion";
                case Metal.Rankless:
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
        public static Discord.Color MetalColor(Metal m)
        {
            switch (m)
            {
                case Metal.Copper:
                    return new Discord.Color(0xb8, 0x73, 0x33); // #b87333
                case Metal.Bronze:
                    return new Discord.Color(0xcd, 0x7f, 0x32); // #cd7f32
                case Metal.Silver:
                    return new Discord.Color(0xc0, 0xc0, 0xc0); // #c0c0c0
                case Metal.Gold:
                    return new Discord.Color(0xff, 0xd7, 0x00); // #ffd700
                case Metal.Platinum:
                    return new Discord.Color(0x00, 0xf9, 0xff); // #00f9ff
                case Metal.Diamond:
                    return new Discord.Color(0xa4, 0x7d, 0xf4); // a47df4
                case Metal.Champion:
                    return new Discord.Color(0xdd, 0x5d, 0xb0); // dd5db0
                case Metal.Rankless:
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
        public static string CompactPrint(Metal m)
        {

            switch (m)
            {
                case Metal.Copper:
                    return "Copper";
                case Metal.Bronze:
                    return "Bronze";
                case Metal.Silver:
                    return "Silver";
                case Metal.Gold:
                    return "Gold";
                case Metal.Platinum:
                    return "Plat";
                case Metal.Diamond:
                    return "Dia";
                case Metal.Champion:
                    return "Champ";
                case Metal.Rankless:
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
        public static string InitialPrint(Metal m)
        {
            switch (m)
            {
                case Metal.Copper:
                    return "C";
                case Metal.Bronze:
                    return "B";
                case Metal.Silver:
                    return "S";
                case Metal.Gold:
                    return "G";
                case Metal.Platinum:
                    return "P";
                case Metal.Diamond:
                    return "D";
                case Metal.Champion:
                    return "CH";
                case Metal.Rankless:
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
            Rank ret = new Rank(Metal.Undefined, 0);
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
        public Metal met;
        public int level = 0;

        public Rank(Metal m, int l)
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
            if (met == Metal.Undefined || met == Metal.Rankless || met == Metal.Champion)
            {
                return false;
            }
            return true;
        }
        public string FullPrint()
        {
            string output = Ranking.MetalPrint(met);
            if (Digits())
            {
                output = output + " " + level;
            }
            return output;
        }

        public override string ToString() => FullPrint();
        public string CompactMetalPrint()
        {
            return Ranking.CompactPrint(met);
        }
        public string CompactFullPrint()
        {
            string output = Ranking.CompactPrint(met);
            if (Digits())
            {
                output = output + " " + level;
            }
            return output;
        }

        public string SpectralMetalPrint()
        {
            return Ranking.InitialPrint(met);
        }

        public string SpectralFullPrint()
        {
            string output = Ranking.InitialPrint(met);
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
}
