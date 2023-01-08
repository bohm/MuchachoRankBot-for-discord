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
using System.Reactive.Joins;

namespace RankBot;

class UbisoftRank
{
    public int rank;
    public double mmr;
    public int wins;
    public int losses;

    public Rank ToRank()
    {
        return RankingV5.RankComputation((int)mmr, wins + losses);
    }
}

public class UbisoftRankedProfile
{
    public string board_id;
    public string id;
    public int max_rank;
    public int max_rank_points;
    public string platform_family;
    public int rank;
    public int rank_points;
    public int season_id;
    public int top_rank_position;
}

public class UbisoftSeasonStatistics
{
    public int deaths;
    public int kills;
    public Dictionary<string, int> match_outcomes;
}

public class UbisoftFullBoard
{
    public UbisoftRankedProfile profile;
    public UbisoftSeasonStatistics season_statistics;
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
    public bool Online = false;
    private readonly HttpClient _httpClient = new HttpClient();
    private UbisoftAuth _token;
    private Timer _reAuthTimer;
    public UbisoftApi()
    {
    }

    public async Task DelayedInit()
    {
        if (!Settings.ApiOfflineMode)
        {
            await ReAuth();
        }
    }

    public async Task ReAuth()
    {
        // We set Online to be false when re-authenticating.
        Online = false;
        // Remark: Since we are running this in a separate thread, a locking for the token API might be needed.
        // On the other hand, the string "_token.ticket" should be valid at all times, as we reauthorize
        // one minute before expiration. As long as only queries to "_token.ticket" are made, locking is not required.

        _token = await LogIn();
        DateTime expiration = DateTime.Parse(_token.expiration);
        TimeSpan untilReauth = expiration - DateTime.Now;
        untilReauth -= TimeSpan.FromSeconds(60);
        if (untilReauth.TotalMinutes < 0 || untilReauth.TotalMinutes > 300)
        {
            throw new Exception("Reauth time computation is outside the expected bounds.");
        }  
        Console.WriteLine($"Logged in to the Ubisoft API. We will reauth in {untilReauth.TotalMinutes} minutes.");
        Online = true;
        _reAuthTimer = new Timer(async x => { await this.ReAuth(); }, null, untilReauth, Timeout.InfiniteTimeSpan);
    }

    public async Task<UbisoftAuth> LogIn()
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(Secret.UbisoftEmail + ":" + Secret.UbisoftPassword);
        string encodedUP = System.Convert.ToBase64String(plainTextBytes);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://public-ubiservices.ubi.com/v3/profiles/sessions");
        request.Headers.TryAddWithoutValidation("Authorization", "Basic " + encodedUP);
        request.Headers.TryAddWithoutValidation("Ubi-AppId", Settings.UbisoftAppId);
        request.Content = new StringContent("", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        JsonTextReader reader = new JsonTextReader(new StringReader(responseString));
        JsonSerializer serializer = new JsonSerializer();
        UbisoftAuth token = serializer.Deserialize<UbisoftAuth>(reader);

        return token;
    }

    public async Task<UbisoftFullBoard> QueryRankPoints(string uplayUuid)
    {
        if(!Online)
        {
            return null;
        }

        var url = $"https://public-ubiservices.ubi.com/v2/spaces/0d2ae42d-4c27-4cb7-af6c-2099062302bb/title/r6s/skill/full_profiles?profile_ids={uplayUuid}&platform_families=pc";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", "Ubi_v1 t=" + _token.ticket);
        request.Headers.TryAddWithoutValidation("Ubi-AppId", Settings.UbisoftAppId);
        request.Headers.TryAddWithoutValidation("Ubi-SessionId", uplayUuid);
        request.Content = new StringContent("", Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new UbisoftApiException();
        }

        var responseString = await response.Content.ReadAsStringAsync();

        // The response is as of today (2022-12-29) quite ugly and requires substantial parsing to recover the current RP value.
        JObject responseJson = JObject.Parse(responseString);
        JToken rankedToken = responseJson.SelectToken("platform_families_full_profiles[0].board_ids_full_profiles[?(@.board_id == 'ranked')].full_profiles[0]");

        UbisoftFullBoard fb = rankedToken.ToObject<UbisoftFullBoard>();
        // string reserialization = rankedToken.ToString();
        // UbisoftFullBoard fb = JsonConvert.DeserializeObject<UbisoftFullBoard>(reserialization);
        return fb;

    }

    public async Task<UbisoftUserResponse> QueryUserByNickname(string uplayNickname)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://public-ubiservices.ubi.com/v2/profiles?platformType=uplay&nameOnPlatform=" + uplayNickname);
        request.Headers.TryAddWithoutValidation("Authorization", "Ubi_v1 t=" + _token.ticket);
        request.Headers.TryAddWithoutValidation("Ubi-AppId", Settings.UbisoftAppId);
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
        request.Headers.TryAddWithoutValidation("Ubi-AppId", Settings.UbisoftAppId);
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
        request.Headers.TryAddWithoutValidation("Ubi-AppId", Settings.UbisoftAppId);
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
        else if (response.profiles.Count == 0)
        {
            throw new RankParsingException("No profile with the nickname found.");
        }
        else if (response.profiles.Count > 1)
        {
            throw new RankParsingException("The number of profiles found with this name is larger than one.");
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