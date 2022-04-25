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
                return Ranking.MMRToRank((int)mmr);
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
            await ReAuth();
        }

        public async Task ReAuth()
        {
            // Remark: Since we are running this in a separate thread, a locking for the token API might be needed.
            // On the other hand, the string "_token.ticket" should be valid at all times, as we reauthorize
            // one minute before expiration. As long as only queries to "_token.ticket" are made, locking is not required.

            _token = await LogIn();
            DateTime expiration = DateTime.Parse(_token.expiration);
            TimeSpan untilReauth = expiration - DateTime.Now;
            untilReauth -= TimeSpan.FromSeconds(60);
            if (untilReauth.Minutes < 0 || untilReauth.Minutes > 60)
            {
                throw new Exception("Reauth time computation failed.");
            }  
            Console.WriteLine($"Logged in to the Ubisoft API. We will reauth in {untilReauth.Minutes} minutes.");
            Timer reAuthTimer = new Timer(async x => { await this.ReAuth(); }, null, untilReauth, Timeout.InfiniteTimeSpan);
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

        public async Task<UbisoftUserResponse> QueryUserByNickname(string uplayNickname)
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
            UbisoftUserResponse responseObject = JsonConvert.DeserializeObject<UbisoftUserResponse>(responseString);


            if (responseObject == null)
            {
                throw new RankParsingException("The response object was not converted correctly.");
            }
            return responseObject;
        }

        public async Task<UbisoftUserResponse> QueryUserByUplayId(string uplayId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://public-ubiservices.ubi.com/v2/profiles?platformType=uplay&userId=" + uplayId);
            request.Headers.TryAddWithoutValidation("Authorization", "Ubi_v1 t=" + _token.ticket);
            request.Headers.TryAddWithoutValidation("Ubi-AppId", "39baebad-39e5-4552-8c25-2c9b919064e2");
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new RankParsingException($"The response for querying the user is {response.StatusCode}, not OK.");
            }
            var responseString = await response.Content.ReadAsStringAsync();
            UbisoftUserResponse responseObject = JsonConvert.DeserializeObject<UbisoftUserResponse>(responseString);


            if (responseObject == null)
            {
                throw new RankParsingException("The response object was not converted correctly.");
            }
            return responseObject;
        }

        public async Task<UbisoftRankResponse> QueryMultipleRanks(HashSet<string> uplayIds)
        {
            StringBuilder sb = new StringBuilder();
            // URL base.
            sb.Append("https://public-ubiservices.ubi.com/v1/spaces/5172a557-50b5-4665-b7db-e3f2e8c5041d/sandboxes/OSBOR_PC_LNCH_A/r6karma/players?board_id=pvp_ranked&season_id=-1&region_id=ncsa&profile_ids=");

            bool first = true;
            foreach (string uplayId in uplayIds)
            {
                if (!first)
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


        // Transition functions. We can get rid of those later, if useful.

        public async Task<string> GetID(string uplayNick)
        {
            UbisoftUserResponse response = await QueryUserByNickname(uplayNick);
            if (response.profiles == null)
            {
                throw new RankParsingException("The response is malformed for some reason.");
            }
            if (response.profiles.Count != 1)
            {
                throw new RankParsingException("The number of profiles is larger than one, so we cannot continue.");
            }

            UbisoftUser curUser = response.profiles.First();
            return curUser.userId;
        }

        public async Task<Rank> GetRank(string uplayId)
        {
            UbisoftRank ubiRank = await QuerySingleRank(uplayId);
            return ubiRank.ToRank();
        }

        public async Task<int> GetMMR(string uplayId)
        {
            UbisoftRank ubiRank = await QuerySingleRank(uplayId);
            int mmr = (int)ubiRank.mmr;
            return mmr;
        }

        public async Task<string> GetUplayName(string uplayId)
        {
            UbisoftUserResponse response = await QueryUserByUplayId(uplayId);
            if (response.profiles == null)
            {
                throw new RankParsingException("The response is malformed for some reason.");
            }
            if (response.profiles.Count != 1)
            {
                throw new RankParsingException("The number of profiles is larger than one, so we cannot continue.");
            }

            UbisoftUser curUser = response.profiles.First();
            return curUser.nameOnPlatform;
        }

    }
}