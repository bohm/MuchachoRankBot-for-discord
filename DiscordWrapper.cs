using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;


namespace R6RankBot
{
    class DiscordWrapper
    {
        // The guild we are operating on. We start doing things only once this is no longer null.
        public Discord.WebSocket.SocketGuild ResidentGuild;


        /// <summary>
        /// Remove all rank roles from a user.
        /// </summary>
        /// <param name="User">A SocketGuildUser object representing the user.</param>
        /// <returns></returns>
        public async Task ClearAllRanks(Discord.WebSocket.SocketGuildUser User)
        {
            System.Console.WriteLine("Removing all ranks from user " + User.Username);
            foreach (var RankRole in User.Roles.Where(x => Ranking.LoudMetalRoles.Contains(x.Name) || Ranking.LoudDigitRoles.Contains(x.Name)
                                                        || Ranking.SpectralMetalRoles.Contains(x.Name) || Ranking.SpectralDigitRoles.Contains(x.Name)))
            {
                await User.RemoveRoleAsync(RankRole);
            }
        }

        public async Task RemoveLoudRoles(Discord.WebSocket.SocketUser Author)
        {
            // If the user is in any mentionable rank roles, they will be removed.
            var Us = (Discord.WebSocket.SocketGuildUser)Author;
            // For each loud role, which can be a loud big role or a loud tiny role:
            foreach (var LoudRole in Us.Roles.Where(x => Ranking.LoudDigitRoles.Contains(x.Name) || Ranking.LoudMetalRoles.Contains(x.Name)))
            {
                await Us.RemoveRoleAsync(LoudRole);
            }
        }

        public async Task AddLoudRoles(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketUser Author, Rank rank)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add the non-digit role.
            string nonDigitLoudName = rank.CompactMetalPrint();
            var LoudRole = Guild.Roles.FirstOrDefault(x => x.Name == nonDigitLoudName);
            if (LoudRole != null)
            {
                await Us.AddRoleAsync(LoudRole);
            }

            // Then, if the rank has a digit role, add that too.
            if (rank.Digits())
            {
                string digitLoudName = rank.CompactFullPrint();
                var LoudDigitRole = Guild.Roles.FirstOrDefault(x => x.Name == digitLoudName);
                if (LoudDigitRole != null)
                {
                    await Us.AddRoleAsync(LoudDigitRole);
                }
            }
        }

        public async Task AddSpectralRoles(Discord.WebSocket.SocketGuild Guild, Discord.WebSocket.SocketUser Author, Rank rank)
        {
            var Us = (Discord.WebSocket.SocketGuildUser)Author;

            // First add the non-digit role.
            string nonDigitSpectralName = rank.SpectralMetalPrint();
            var spectralRole = Guild.Roles.FirstOrDefault(x => x.Name == nonDigitSpectralName);
            if (spectralRole != null)
            {
                await Us.AddRoleAsync(spectralRole);
            }

            // Then, if the rank has a digit role, add that too.
            if (rank.Digits())
            {
                string digitSpectralName = rank.SpectralFullPrint();
                var digitSpectralRole = Guild.Roles.FirstOrDefault(x => x.Name == digitSpectralName);
                if (digitSpectralRole != null)
                {
                    await Us.AddRoleAsync(digitSpectralRole);
                }
            }
        }

        public async Task<Rank> GetCurrentRank(string R6TabID)
        {
            R6TabDataSnippet data = await TRNHttpProvider.GetData(R6TabID);
            Rank r = data.ToRank();
            return r;
        }
        // --- End of static section. ---
    }
}
