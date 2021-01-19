using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace R6RankBot
{
    class R6TabDataSnippet
    {
        public int mmr;
        public int rank;

        public R6TabDataSnippet(int v1, int v2)
        {
            this.mmr = v1;
            this.rank = v2;
        }

        public Rank ToRank()
        {
            if (rank == 0)
            {
                return new Rank(Metal.Unranked, 0);
            }
            else
            {
                return Ranking.MMRToRank(mmr);
            }
        }
    }


    class TRNHttpProvider
    {

        public static string FetchAfterMatch(string data, string needle, int length)
        {
            int position = data.IndexOf(needle);
            if (position <= 0) // TODO: the span tag may not exist on low-level accounts, so handle that more gracefully.
            {
                return "";
            }

            string resultString = data.Substring(position + needle.Length, length);
            return resultString;
        }

        public static string FetchAfterMatch(string data, string needle, char delimeter)
        {
            int position = data.IndexOf(needle);
            if (position <= 0) // TODO: the span tag may not exist on low-level accounts, so handle that more gracefully.
            {
                return "";
            }

            string resultPrefix = data.Substring(position + needle.Length);
            int delimeterPosition = resultPrefix.IndexOf(delimeter);
            if (delimeterPosition <= 0)
            {
                return "";
            }

            return resultPrefix.Substring(0, delimeterPosition);
        }

        public static async Task<R6TabDataSnippet> GetData(string TRNId)
        {
            string url = "https://r6.tracker.network/profile/id/" + TRNId;
            HttpClient website = new HttpClient();
            var response = await website.GetAsync(url);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException("The HTTP response did not return OK or was empty.");
            }
            string websiteText = await response.Content.ReadAsStringAsync();

            // Check if user exists by matching some tag that is always present for all existing accounts.

            const string userSiteMatch = "<trn-profile-header-favorite platform=\"4\" ";
            const string followUp = "nickname";
            string checkMatch = FetchAfterMatch(websiteText, userSiteMatch, 8);
            if (!followUp.Equals(checkMatch))
            {
                throw new RankParsingException("The returned page does not seem to be a user page");
            }

            const string matchingString = "<span class=\"trn-text--dimmed\">Skill </span>\n<span>\n";
            string MMRstring = FetchAfterMatch(websiteText, matchingString, 5);

            if (MMRstring.Length == 0)
            {
                // Return the user as rankless, possibly did not play any ranked games yet.
                return new R6TabDataSnippet(0, 0);
            }

            // Console.WriteLine("Found the string: \"" + MMRstring + "\"");
            float floatMMR;
            // float.Parse(MMRstring); // I prefer the correct parsing.
            // bool success = float.TryParse(MMRstring, out floatMMR); // I said the correct parsing.
            bool success = float.TryParse(MMRstring, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out floatMMR); // Perfection.

            if (!success)
            {
                throw new RankParsingException("Parsing MMR to float failed.");
            }
            floatMMR *= 100;
            int mmr = (int)floatMMR;

            // Console.WriteLine("MMR found: " + mmr);

            // Locate whether the tracker thinks the user has a rank (had >= 10 matches).

            const string matchingRankName = "<div class=\"trn-defstat mb0\">\n<div class=\"trn-defstat__name\">Rank</div>\n<div class=\"trn-defstat__value\">\n";
            const string ranklessResponse = "Not ranked yet.";
            string rankString = FetchAfterMatch(websiteText, matchingRankName, ranklessResponse.Length);

            // Console.WriteLine("Found the rank string: \"" + rankString + "\"");


            int rank = 1;
            if (ranklessResponse.Equals(rankString) || rankString.Length == 0)
            {
                rank = 0;
                // Console.WriteLine("This user is rankless.");
            }
            else
            {
                // Console.WriteLine("This user has a rank.");
            }

            return new R6TabDataSnippet(mmr, rank); // TODO: fix.
        }

        /// <summary>
        /// This function also asks for a data refresh, if the provider has that option. Needs to be called sparingly.
        /// TRN has no possibility of updating, so we just call get data.
        /// </summary>
        /// <param name="r6TabId"></param>
        /// <returns></returns>
        public static async Task<R6TabDataSnippet> UpdateAndGetData(string TRNID)
        {
            return await GetData(TRNID);
        }


        public static async Task<string> GetID(string UplayNick)
        {
            string url = "https://r6.tracker.network/profile/pc/" + UplayNick;
            HttpClient website = new HttpClient();
            var response = await website.GetAsync(url);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException("The HTTP response did not return OK or was empty.");
            }

            string websiteText = await response.Content.ReadAsStringAsync();
            const string IDMatch = "<a class=\"trn-button trn-button--primary mt24\" target=\"_blank\" href=\"https://r6.tracker.network/profile/id/";
            string probableID = FetchAfterMatch(websiteText, IDMatch, '\"');

            // Console.WriteLine("It seems the ID of " + UplayNick + " is " + probableID);
            return probableID;

        }
    }

        /// <summary>
        ///  A minimal API for reaching R6Tab and getting the required info.
        /// </summary>
        class R6Tab
    {
        /// <summary>
        ///  Convert a JObject into a list of the JTokens contained therein.
        ///  Used to bypass the ridiculous JSON API of R6Tab where an array
        ///  of results is instead presented as a single object with properties being the data.
        /// </summary>
        /// <param name="r6TabId"></param>
        /// <returns></returns>
        public static List<JToken> ObjectToList(JObject obj)
        {
            List<JToken> ret = new List<JToken>();
            foreach (JProperty pair in obj.Children<JProperty>())
            {
                ret.Add((JToken) pair.Value);
            }
            return ret;
        }

        public static string StripGarbageFromPName(string r6TabPName)
        {
            // Ubisoft nicknames should not contain a whitespace
            string[] spl = r6TabPName.Split(' ');
            return spl[0]; // The first part should be the important one.
        }

        public static async Task<R6TabDataSnippet> GetData(string r6TabId)
        {
            try
            {
                if (r6TabId == null)
                {
                    throw new RankParsingException();
                }
                // The R6Tab API now requires an access token, so we provide it via the URL.
                string url = "https://r6.apitab.com/player/" + r6TabId + "?cid=" + Secret.r6TabToken;
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                string responseText = null;

                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    responseText = await response.Content.ReadAsStringAsync();
                }

                if (responseText == null)
                {
                    throw new RankParsingException();
                }

                JObject joResponse = JObject.Parse(responseText);

                JValue userFound = (JValue)joResponse["found"];
                if (!userFound.ToObject<bool>())
                {
                    throw new RankParsingException();
                }

                JObject rankData = (JObject)joResponse["ranked"];
                JValue MMR = (JValue)rankData["mmr"];
                JValue rank = (JValue)rankData["rank"];

                return new R6TabDataSnippet(MMR.ToObject<int>(), rank.ToObject<int>());
            }
            catch (TaskCanceledException)
            {
                throw new RankParsingException();
            }

        }

        /// <summary>
        /// Queries the tabstats API, but also calls for a refresh. Needs to be called sparingly.
        /// </summary>
        /// <param name="r6TabId"></param>
        /// <returns></returns>
        public static async Task<R6TabDataSnippet> UpdateAndGetData(string r6TabId)
        {
            try
            {
                if (r6TabId == null)
                {
                    throw new RankParsingException();
                }
                // The R6Tab API now requires an access token, so we provide it via the URL.
                string url = "https://r6.apitab.com/update/" + r6TabId + "?cid=" + Secret.r6TabToken;
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                string responseText = null;

                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    responseText = await response.Content.ReadAsStringAsync();
                }

                if (responseText == null)
                {
                    throw new RankParsingException();
                }

                JObject joResponse = JObject.Parse(responseText);

                JValue userFound = (JValue)joResponse["found"];
                if (!userFound.ToObject<bool>())
                {
                    throw new RankParsingException();
                }

                JObject rankData = (JObject)joResponse["ranked"];
                JValue MMR = (JValue)rankData["mmr"];
                JValue rank = (JValue)rankData["rank"];

                return new R6TabDataSnippet(MMR.ToObject<int>(), rank.ToObject<int>());
            }
            catch (TaskCanceledException)
            {
                throw new RankParsingException();
            }

        }


        public static async Task<string> GetTabID(string UplayNick)
        {
            try
            {
                // The R6Tab API now requires an access token, so we provide it via the URL.
                string url = "https://r6.apitab.com/search/uplay/" + UplayNick + "?cid=" + Secret.r6TabToken;
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                string responseText = null;
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    responseText = await response.Content.ReadAsStringAsync();
                }

                JObject joResponse = JObject.Parse(responseText);

                // we ignore foundmatch for now, because it is quite unreliable
                /*
                JValue foundmatch = (JValue)joResponse["foundmatch"];
                if (!foundmatch.ToObject<bool>())
                {
                    throw new RankParsingException("Foundmatch could not be found or converted to bool.");
                } */

                string tabID = "";
                List<JToken> playerlist;

                try {
                    playerlist = ObjectToList((JObject)joResponse["players"]);
                }
                catch (InvalidCastException)
                {
                    throw new RankParsingException("The playerlist was not provided, likely wrong nickname.");
                }

                bool matched = false;
                StringBuilder allNickString = new StringBuilder(); // All nicks concatenated for debug purposes.

                if (playerlist.Count == 0)
                {
                    throw new RankParsingException("Playerlist is empty, no match found.");
                }
                foreach (JToken playerToken in playerlist)
                {
                    // Get nickname as reported by r6tab, check for an exact match with the uplay nickname given.
                    JObject playerValue = (JObject)playerToken;
                    JValue nickVal = (JValue)playerValue["profile"]["p_name"];
                    string nickBeforeParse = nickVal.ToObject<string>();
                    string nick = StripGarbageFromPName(nickBeforeParse);
                    allNickString.Append(nick);

                    if (nick.Equals(UplayNick))
                    {
                        matched = true;
                        JValue tabIDVal = (JValue)playerValue["profile"]["p_user"];
                        tabID = tabIDVal.ToObject<string>();
                    }
                }

                if (!matched)
                {
                    
                    throw new RankParsingException("No suitable match; playerlist" + allNickString.ToString());
                }

                if (tabID.Length == 0)
                {
                    throw new RankParsingException("Match found, tabID parsing failed.");
                }

                return tabID;

            }
            catch (TaskCanceledException)
            {
                throw new RankParsingException();
            }
        }
    }
}
