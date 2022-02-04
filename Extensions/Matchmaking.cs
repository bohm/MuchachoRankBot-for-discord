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
    
    class EqualPartitionBuilder
    {
        public List<List<List<int>>> PartitionChoices;
        public ulong ChoicesCounter = 0;
        public List<List<int>> TeamLeaderChoices;
        public int PartSize = 0;
        public int PartNumber = 0;
        public int ElCount = 0;
        public int StoredBestVal = 0;
        private Evaluator _eval;
        public List<List<int>> StoredBest;
        public EqualPartitionBuilder(int partSize, int elementCount, Evaluator ev)
        {

            PartSize = partSize;
            ElCount = elementCount;
            TeamLeaderChoices = new List<List<int>>();
            PartitionChoices = new List<List<List<int>>>();
            _eval = ev;

            if (ElCount % partSize != 0)
            {
                throw new Exception("We cannot split into equal sizes, aborting.");
            }

            PartNumber = ElCount / partSize;

        }

        public void ComputeLeaderChoices()
        {
            // We actually make use of the fact that the people are numbered 0...19, in order to save access into _els.


            // Zero is the team leader of the first team always.

            List<int> selectionStart = new List<int>(PartNumber);
            selectionStart.Add(0);
            RecursiveTeamLeaders(selectionStart, 1, 1);
        }

        private void RecursiveTeamLeaders(List<int> currentSelection, int team, int elemPosition)
        {
            if (team == PartNumber)
            {
                TeamLeaderChoices.Add(currentSelection.ToList());
                return;
            }

            if ( elemPosition > team*PartSize)
            {
                // This person cannot be a team leader, because all lower numbers
                // (elemPosition of them) must be in the team*_partSize previous teams.
                return;
            }

            currentSelection.Add(elemPosition);
            RecursiveTeamLeaders(currentSelection, team + 1, elemPosition + 1);
            currentSelection.RemoveAt(currentSelection.Count - 1);
            RecursiveTeamLeaders(currentSelection, team, elemPosition + 1);
        }


        private void RecursiveBestWithLeaders(List<List<int>> currentPartition, List<int> teamLeaders, int team, int elemPosition)
        { 
            if (elemPosition == ElCount)
            {
                // Everybody is assigned and no error was reached, we can use the evaluation function

                int value = _eval.Evaluate(currentPartition);
                if (StoredBest == null || value < StoredBestVal)
                {
                    StoredBest = currentPartition.Select(l => l.ToList()).ToList();
                    StoredBestVal = value;
                }

                //PartitionChoices.Add( currentPartition.Select(l => l.ToList()).ToList() );
                
                ChoicesCounter++;
                return;
            }

            if (team >= PartNumber)
            {
                // We cannot add this element to any team, just return;
                return;
            }

            if (teamLeaders.Contains(elemPosition))
            {
                // Do not assign any team leaders, they are hardcoded.
                RecursiveBestWithLeaders(currentPartition, teamLeaders, team, elemPosition + 1);
                return;
            }


            if (teamLeaders[team] > elemPosition)
            {
                // Teamleader is strictly bigger, so this element cannot go into this team (or any later);
                return;
            }

            if (currentPartition[team].Count < PartSize)
            {

                // The team is not full, try to assign this element to this team.
                currentPartition[team].Add(elemPosition);
                RecursiveBestWithLeaders(currentPartition, teamLeaders, 0, elemPosition + 1);
                currentPartition[team].RemoveAt(currentPartition[team].Count - 1);
            }

            // Try to add this element to other teams
            RecursiveBestWithLeaders(currentPartition, teamLeaders, team + 1, elemPosition);
            // It has to go into som team, so there is no need for different recursion.
        }

        public void Build()
        {
            ComputeLeaderChoices();
            StoredBestVal = int.MaxValue;
            StoredBest = null;

            foreach (var leaders in TeamLeaderChoices)
            {
                List<List<int>> partitionStart = new List<List<int>>();
                foreach(var element in leaders)
                {
                    List<int> soloLeader = new List<int>();
                    soloLeader.Add(element);
                    partitionStart.Add(soloLeader);
                }

                RecursiveBestWithLeaders(partitionStart, leaders, 0, 0);
            }
        }
    }

    class Evaluator
    {
        private Dictionary<int, int> _mmrs;
        public Evaluator(List<(int, int)> playerMMR)
        {
            _mmrs = new Dictionary<int, int>();

            foreach (var tuple in playerMMR)
            {
                Console.WriteLine($"Adding player {tuple.Item1} with {tuple.Item2} MMR to the mapping");
                _mmrs.Add(tuple.Item1, tuple.Item2);
            }
        }

        public int SumTeam(List<int> team)
        {
            int sum = 0;
            foreach (var x in team)
            {
                sum += _mmrs[x];
            }
            return sum;
        }

        public string PrintTeam(List<int> team)
        {
            bool first = true;

            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var player in team)
            {
                if (first)
                {
                    first = false;
                } else
                {
                    sb.Append(", ");
                }

                sb.Append(player);
            }
            sb.Append("]");
            return sb.ToString();

        }
        public int Evaluate(List<List<int>> partition)
        {
            int minTeam = SumTeam(partition[0]);
            int maxTeam = SumTeam(partition[1]);

            foreach (var team in partition)
            {
                int teamVal = SumTeam(team);
                if (teamVal > maxTeam)
                {
                    maxTeam = teamVal;
                }

                if (teamVal < minTeam)
                {
                    minTeam = teamVal;
                }
            }

            //bool first = true;
            //foreach(var team in partition)
            //{
            //    if(first)
            //    {
            //        first = false;
            //    }
            //    else
            //    {
            //        Console.Write(" vs. ");
            //    }
            //    Console.Write($"{PrintTeam(team)}, {SumTeam(team)} MMR");
            //}

            //Console.WriteLine($"Difference is {maxTeam - minTeam}");
            return maxTeam - minTeam;
        }
    }

    class Matchmaking
    {

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

        public static string TeamString(List<int> team, int num, Evaluator ev, List<string> playerNames)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Team {num}: ");
            bool first = true;

            foreach (var player in team)
            {
                if (!first)
                {
                    sb.Append(" ");
                }
                else
                {
                    first = false;
                }
                sb.Append(playerNames[player]);
            }

            sb.Append(" // MMR average: ");
          
            sb.Append(Math.Floor((ev.SumTeam(team) / 5.0)));
            return sb.ToString();
        }

        public async Task BuildTeams(Bot bot, ulong sourceGuild, Discord.WebSocket.ISocketMessageChannel channel, params string[] tenOrTwenty)
        {
            List<string> playerNames = new List<string>();
            List<ulong> playerIDs = new List<ulong>();
            List<(int, int)> playerMMR = new List<(int, int)>();

            // The command actually checks this first, but let's also check this for the sake of consistency.
            if (tenOrTwenty.Length != 10 && tenOrTwenty.Length != 20)
            {
                await channel.SendMessageAsync("There is not 10 or 20 people, we cannot matchmake.");
                return;
            }

            int players = tenOrTwenty.Length;

            if (!bot.guilds.byID.ContainsKey(sourceGuild))
            {
                throw new GuildStructureException("Something went wrong -- the selected guild ID was not found before matchmaking.");
            }

            DiscordGuild guild = bot.guilds.byID[sourceGuild];
            foreach (string username in tenOrTwenty)
            {
                SocketGuildUser person = guild.GetSingleUser(username);
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


            for (int i = 0; i < players; i++)
            {
                string player = playerNames[i];
                ulong disId = playerIDs[i];
                string uplayId = bot._data.DiscordUplay[disId];

                int mmr = -1;
                try
                {
                    mmr = await TRNHttpProvider.GetCurrentMMR(uplayId);
                }
                catch (Exception)
                {
                    await channel.SendMessageAsync($"Error while fetching r6.tracker.network data for {player}.");
                    return;
                }

                if (mmr < 0)
                {
                    await channel.SendMessageAsync($"Error while fetching r6.tracker.network data for {player}.");
                    return;
                }

                playerMMR.Add((i, mmr));
            }

            Evaluator maxMin = new Evaluator(playerMMR);

            // With MMRs loaded, we can finally do some balancing.
            EqualPartitionBuilder eb = new EqualPartitionBuilder(5, players, maxMin);
            eb.Build();

            List<List<int>> bestPartitions = eb.StoredBest;

            await channel.SendMessageAsync("Best choice:");
            int teamId = 0;
            foreach(var team in bestPartitions)
            {
                await channel.SendMessageAsync(TeamString(team, teamId, maxMin, playerNames));
                teamId++;
            }

            /*
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
            */
        }

    }
}
