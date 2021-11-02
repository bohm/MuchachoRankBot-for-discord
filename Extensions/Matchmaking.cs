using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RankBot.Extensions
{
    class SubsetBuilder
    {
        public List<(int, int)> players;
        public List<List<(int, int)> > allSubsets;

        public SubsetBuilder(List<(int, int)> p)
        {
            players = p;
            allSubsets = new List<List<(int, int)> >();
        }

        public void Build(int subsetSize)
        {
            RecursiveBuild(new List<(int, int)>(), 0, subsetSize);
        }

        private void RecursiveBuild(List<(int, int)> currentSelection, int position, int remaining)
        {
            if (remaining == 0)
            {
                allSubsets.Add(currentSelection.ToList());
            } else
            {
                if (position >= players.Count)
                {
                    return;
                }

                currentSelection.Add(players[position]);
                RecursiveBuild(currentSelection, position + 1, remaining-1);
                currentSelection.RemoveAt(currentSelection.Count - 1);
                RecursiveBuild(currentSelection, position + 1, remaining);
            }
        }
    }

    class Matchmaking
    {
        // Matchmaking should technically be an extension. Maybe consider writing it into a separate file.

        private List<(int, int)> playerMMRs = new List<(int, int)>();
        private List<string> playerNames = new List<string>();
        private List<ulong> playerIDs = new List<ulong>();
        private int totalMMR = 0;


        public int SumTeam(List<(int, int)> team)
        {
            int sum = 0;
            foreach (var x in team)
            {
                sum += x.Item2;
            }
            return sum;
        }

        public (int, int) TeamMMRs(List<(int, int)> oneTeam)
        {
            int sum = SumTeam(oneTeam);
            return (sum, totalMMR - sum);
        }

        public int TeamAbsDiff(List<(int, int)> oneTeam)
        {
            var (x, y) = TeamMMRs(oneTeam);
            return Math.Abs(x - y);
        }

        public int CompareTwoChoices(List<(int, int)> team1, List<(int, int)> team2)
        {
            return TeamAbsDiff(team1).CompareTo(TeamAbsDiff(team2));
        }

        public string NamesFromPlayers(List<(int, int)> team, List<string> playerNames)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;

            foreach(var (id, mmr) in team)
            {
                if (!first)
                {
                    sb.Append(" ");
                } else
                {
                    first = false;
                }
                sb.Append(playerNames[id]);
            }

            return sb.ToString();
        }

        public async Task BuildTeams(Bot bot, ulong sourceGuild, Discord.WebSocket.ISocketMessageChannel channel, params string[] tenPeople)
        {
            List<string> playerNames = new List<string>();
            List<ulong> playerIDs = new List<ulong>();
            List<(int, int)> playerMMR = new List<(int, int)>();

            // The command actually checks this first, but let's also check this for the sake of consistency.
            if (tenPeople.Length != 10)
            {
                await channel.SendMessageAsync("There is not 10 people, we cannot matchmake.");
                return;
            }

            foreach (string username in tenPeople)
            {

                SocketGuildUser person = bot.GetGuildUser(username, sourceGuild);
                if (person == null)
                {
                    await channel.SendMessageAsync($"The name \"{username}\" not matched to a Discord user.");
                    return;
                }
                playerNames.Add(username);
                playerIDs.Add(person.Id);

                // Before trying to check MMR first, check if users are even tracked.

                if (!bot._data.DiscordUplay.ContainsKey(person.Id))
                {
                    await channel.SendMessageAsync($"The name \"{username}\" does not seem to be tracked.");
                    return;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                string player = playerNames[i];
                ulong disId = playerIDs[i];
                string uplayId = bot._data.DiscordUplay[disId];
                TrackerDataSnippet data = new TrackerDataSnippet(0, -1);
                try
                {
                    data = await TRNHttpProvider.GetData(uplayId);
                }
                catch (Exception)
                {
                    await channel.SendMessageAsync($"Error while fetching r6.tracker.network data for {player}.");
                    return;
                }

                if (data.rank < 0)
                {
                    await channel.SendMessageAsync($"Error while fetching r6.tracker.network data for {player}.");
                    return;
                }

                if (data.rank == 0)
                {
                    await channel.SendMessageAsync($"Player {player} is Rankless. We treat him as 2500 MMR.");
                    data.mmr = 2500;
                }

                playerMMR.Add((i, data.mmr));
            }

            // Compute the total MMR to quickly evaluate the complement of a team.
            totalMMR = SumTeam(playerMMR);

            // With MMRs loaded, we can finally do some balancing.
            SubsetBuilder sb = new SubsetBuilder(playerMMR);
            sb.Build(5);
            List<List<(int, int)>> allChoices = sb.allSubsets;
            allChoices.Sort(CompareTwoChoices);

            // Get the best choice.
            List<(int, int)> bestChoice = allChoices[0];
            int bestChoiceAverage = SumTeam(bestChoice) / 5;
            List<(int, int)> bestOpponent = playerMMR.Except(bestChoice).ToList();
            int bestOpponentAverage = SumTeam(bestOpponent) / 5;
            string teamNames = NamesFromPlayers(bestChoice, playerNames);
            string opponents = NamesFromPlayers(bestOpponent, playerNames);
            await channel.SendMessageAsync($"Best choice (MMRs {bestChoiceAverage} vs. {bestOpponentAverage}): ");
            await channel.SendMessageAsync($"Team 1: {teamNames}.");
            await channel.SendMessageAsync($"Team 2: {opponents}.");
        }

    }
}
