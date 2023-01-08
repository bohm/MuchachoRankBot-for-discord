using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RankBot;

public class UbisoftApiTesting
{
    public static async Task RunTests()
    {
        const string orsonUplayNickname = "DoctorOrson";
        const string orsonUplayIdString = "93f4f20f-ac19-47fb-afe8-f36662a40b79";

        UbisoftApi uApi = new UbisoftApi();
        await uApi.ReAuth();
        var response = await uApi.QueryUserByNickname(orsonUplayNickname);
        Console.WriteLine($"Test result: DoctorOrson's uplay ID is {response.profiles[0].idOnPlatform}.");
        var userById = await uApi.QueryUserByUplayId(orsonUplayIdString);
        Console.WriteLine($"Test result: {orsonUplayIdString}'s nickname is {response.profiles[0].nameOnPlatform}.");

        // Console.WriteLine(userById.ToString());

        string[] playerNicknames = {"DoctorOrson", "DuoDoctorOrson", "HOUBEX", "Prokop._"};

        foreach (var playerNickname in playerNicknames)
        {
            var resp = await uApi.QueryUserByNickname(playerNickname);
            Console.WriteLine($"Test result: {playerNickname}'s uplay ID is {resp.profiles[0].idOnPlatform}.");

            var rankStructure = await uApi.QueryRankPoints(resp.profiles[0].idOnPlatform);
            var rankDataPoint = new RankDataPointV6(rankStructure);
            var metal = rankDataPoint.ToMetal();
            int rankPoints = rankStructure.profile.rank_points;
            Console.WriteLine($"The user {playerNickname} currently has {rankPoints} Rank Points, corresponding to {RankingV6.MetalPrint(metal)}.");
        }
    }
}