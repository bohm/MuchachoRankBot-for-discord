using System;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace RankBot
{
    namespace StatsDBAPI
    {
        public class RootObject
        {
            [JsonProperty("code")]
            public int Code;
            [JsonProperty("message")]
            public string Message;
            [JsonProperty("timestamp")]
            public int Timestamp;
            [JsonProperty("error")]
            public bool Error;
            [JsonProperty("payload")]
            public PayloadObject Payload;
        }

        public class PayloadObject
        {
            [JsonProperty("system")]
            public SystemObject System;
            [JsonProperty("user")]
            public UserObject User;
        }

        public class UserObject
        {
            [JsonProperty("id")]
            public string uplayId;
            [JsonProperty("nickname")]
            public string uplayNickname;
        }

        public class BanObject
        {
            [JsonProperty("timestamp")]
            public int Timestamp;
            [JsonProperty("ubiBanCode")]
            public int ubiBanCode;
            [JsonProperty("reason")]
            public string Reason;
        }

        public class SystemObject
        {
            [JsonProperty("available")]
            public bool Available;
            [JsonProperty("bans")]
            public List<BanObject> Bans;
        }
    }


    public class StatsDB
	{
        private readonly string Token;
		public StatsDB()
		{
			Token = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(Secret.statsDbId + ":" + Secret.statsDbPassword));
		}

        public static string BuildURL(string oldNickname, string uplayId)
        {
            return $"https://r6db.net/player/{oldNickname}/{uplayId}";
        }

        public (bool, int) CheckUserBan(string uplayId)
        {
            string url = "https://api.statsdb.net/r6/player/" + uplayId;

            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Authorization", "Basic " + Token); //Requires the alternative "X-Authorization" header

            var response = httpClient.SendAsync(request).Result;
            var str = response.Content.ReadAsStringAsync().Result;
            StreamWriter sw = new StreamWriter("statsdb.dump");
            sw.Write(str);
            sw.Close();

            StatsDBAPI.RootObject deserializedOutput = null;
            try
            {
                deserializedOutput = JsonConvert.DeserializeObject<StatsDBAPI.RootObject>(str);
            } catch (JsonReaderException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine($"String to be deserialized: {str}");
            }

            if (deserializedOutput != null && deserializedOutput.Payload != null && deserializedOutput.Payload.System != null)
            {
                if (deserializedOutput.Payload.System.Bans != null)
                {
                    if (deserializedOutput.Payload.System.Bans.Count == 0)
                    {
                        // Everything seems like no ban is detected.
                        return (false, 0);
                    }
                    else
                    {
                        // Mild TODO: We are currently reading the first ban, consider reading all of them or handle softer bans.
                        int ubiBanCode = deserializedOutput.Payload.System.Bans[0].ubiBanCode;
                        return (true, ubiBanCode);
                    }
                }
            } else
            {
                throw new BanParsingException();
            }

            return (false, 0);
        }
    }
}
