using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json; // TODO: Migrate to Microsoft's JSON.
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Globalization;

namespace RankBot
{
    class UbisoftRank
    {
        public int rank;
        public double mmr;
        public int wins;
        public int losses;

        public Rank ToRank()
        {
            if (wins + losses >= 10)
            {
                return Ranking.MMRToRank((int) mmr);
            }
            else
            {
                return new Rank(Metal.Rankless, 0);
            }
        }
    }
    class UbisoftRankResponse
    {
        public Dictionary<string, UbisoftRank> players;
    }

    class UbisoftUser
    {
        public string profileId;
        public string userId;
        public string platformType;
        public string idOnPlatform;
        public string nameOnPlatform;
    }

    class UbisoftUserResponse
    {
        public List<UbisoftUser> profiles;
    }

    class UbisoftAuth
    {
        public string platformType;
        public string ticket;
        public string profileId;
        public string userId;
        public string nameOnPlatform;
        public string expiration;
        public string clientIp;
        public string clientIpCountry;
    }

    class UbisoftApi
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private UbisoftAuth _token;
        public UbisoftApi()
        {
        }

        public async Task DelayedInit()
        {
            _token = await LogIn();
        }

        public async Task<UbisoftAuth> LogIn()
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(Secret.UbisoftEmail + ":" + Secret.UbisoftPassword);
            string encodedUP = System.Convert.ToBase64String(plainTextBytes);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://public-ubiservices.ubi.com/v3/profiles/sessions");
            request.Headers.TryAddWithoutValidation("Authorization", "Basic " + encodedUP);
            request.Headers.TryAddWithoutValidation("Ubi-AppId", "39baebad-39e5-4552-8c25-2c9b919064e2");
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            JsonTextReader reader = new JsonTextReader(new StringReader(responseString));
            JsonSerializer serializer = new JsonSerializer();
            UbisoftAuth token = serializer.Deserialize<UbisoftAuth>(reader);
            
            return token;
        }

        public async Task<string> QueryUplayId(string uplayNickname)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://public-ubiservices.ubi.com/v2/profiles?platformType=uplay&nameOnPlatform=" + uplayNickname);
            request.Headers.TryAddWithoutValidation("Authorization", "Ubi_v1 t=" + _token.ticket);
            request.Headers.TryAddWithoutValidation("Ubi-AppId", "39baebad-39e5-4552-8c25-2c9b919064e2");
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException($"The response for querying the user is {response.StatusCode}, not OK.");
            }
            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString != null)
            {
                throw new RankParsingException("The response string is malformed.");
            }
            return responseString;

        }

        public async Task<UbisoftRankResponse> QueryMultipleRanks(HashSet<string> uplayIds)
        {
            StringBuilder sb = new StringBuilder();
            // URL base.
            sb.Append("https://public-ubiservices.ubi.com/v1/spaces/5172a557-50b5-4665-b7db-e3f2e8c5041d/sandboxes/OSBOR_PC_LNCH_A/r6karma/players?board_id=pvp_ranked&season_id=-1&region_id=ncsa&profile_ids=");

            bool first = true;
            foreach (string uplayId in uplayIds)
            {
                if(!first)
                {
                    sb.Append(",");
                }
                else
                {
                    first = false;
                }
                sb.Append(uplayId);
            }
            var request = new HttpRequestMessage(HttpMethod.Get, sb.ToString());
            request.Headers.TryAddWithoutValidation("Authorization", "Ubi_v1 t=" + _token.ticket);
            request.Headers.TryAddWithoutValidation("Ubi-AppId", "39baebad-39e5-4552-8c25-2c9b919064e2");
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException($"The response for querying the user is {response.StatusCode}, not OK."); // TODO: Better message later.
            }

            var responseString = await response.Content.ReadAsStringAsync();
            UbisoftRankResponse responseObject = JsonConvert.DeserializeObject<UbisoftRankResponse>(responseString);

            if (responseObject == null)
            {
                throw new RankParsingException("Error during response conversion."); // TODO: Better message later.
            }

            return responseObject;


        }

        public async Task<UbisoftRank> QuerySingleRank(string uplayId)
        {
            HashSet<string> set = new HashSet<string> { uplayId };
            UbisoftRankResponse response = await QueryMultipleRanks(set);

            if (response == null || response.players == null)
            {
                throw new RankParsingException("The response object is malformed."); // TODO: Better message later.
            }

            if (response.players.Count != 1)
                throw new RankParsingException("The response object has an incorrect number of players."); // TODO: Better message later.

            return response.players.Values.First();
        }


            

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
            if (position <= 0) // TODO:     the span tag may not exist on low-level accounts, so handle that more gracefully.
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

        public static async Task<TrackerDataSnippet> GetData(string TRNId)
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

            //const string matchingString = "<span class=\"trn-text--dimmed\">Skill </span>\n<span>\n";
            const string matchingString = "<div style=\"font-family: Rajdhani; font-size: 3rem;\">";
            string MMRstring = FetchAfterMatch(websiteText, matchingString, '<');

            if (MMRstring.Length == 0)
            {
                // Return the user as rankless, possibly did not play any ranked games yet.
                return new TrackerDataSnippet(0, 0);
            }

            // Console.WriteLine("Found the string: \"" + MMRstring + "\"");
            int mmr;
            // int.Parse(MMRstring); // I prefer the correct parsing.
            // bool success = int.TryParse(MMRstring, out floatMMR); // I said the correct parsing.
            bool success = int.TryParse(MMRstring, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out mmr); // Perfection.

            if (!success)
            {
                throw new RankParsingException("Parsing MMR to float failed.");
            }

            // Locate whether the tracker thinks the user has a rank (had >= 10 matches).

            const string matchingRankName = "<div class=\"trn-text--dimmed\" style=\"font-size: 1.5rem;\">";
            // const string matchingRankName = "<div class=\"trn-defstat mb0\">\n<div class=\"trn-defstat__name\">Rank</div>\n<div class=\"trn-defstat__value\">\n";
            const string ranklessResponse = "No Rank";
            string rankString = FetchAfterMatch(websiteText, matchingRankName, '<');

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

            return new TrackerDataSnippet(mmr, rank); // TODO: fix.
        }

        /// <summary>
        /// This function also asks for a data refresh, if the provider has that option. Needs to be called sparingly.
        /// TRN has no possibility of updating, so we just call get data.
        /// </summary>
        /// <param name="r6TabId"></param>
        /// <returns></returns>
        public static async Task<TrackerDataSnippet> UpdateAndGetData(string TRNID)
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

        public static async Task<Rank> GetCurrentRank(string UplayID)
        {
            TrackerDataSnippet data = await UbisoftApi.GetData(UplayID);
            Rank r = data.ToRank();
            return r;
        }

        /// <summary>
        /// Queries the tracker to return the current nickname, given the uplay unique ID. An inverse of GetID().
        /// </summary>
        /// <param name="uplayId"></param>
        /// <returns></returns>
        public static async Task<string> GetCurrentUplay(string uplayId)
        {
            string url = "https://r6.tracker.network/profile/id/" + uplayId;
            HttpClient website = new HttpClient();
            var response = await website.GetAsync(url);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException("The HTTP response did not return OK or was empty.");
            }

            string websiteText = await response.Content.ReadAsStringAsync();
            const string nicknameMatch = "<span class=\"trn-profile-header__name\">\n";
            string probableUplayName = FetchAfterMatch(websiteText, nicknameMatch, '\n');
            return probableUplayName;
        }

        public static async Task<int> GetCurrentMMR(string uplayId)
        {
            string url = "https://r6.tracker.network/profile/id/" + uplayId;
            HttpClient website = new HttpClient();
            var response = await website.GetAsync(url);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException("The HTTP response did not return OK or was empty.");
            }

            string websiteText = await response.Content.ReadAsStringAsync();
            const string currentMMRMatch = "<div style=\"font-family: Rajdhani; font-size: 3rem;\">";
            string MMRString = FetchAfterMatch(websiteText, currentMMRMatch, '<');
            if (int.TryParse(MMRString, NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out int MMRint))
            {
                return MMRint;
            }
            else
            {
                throw new RankParsingException($"Could not parse the MMR from a string {MMRString} into an integer.");
            }
        }
    }
}
