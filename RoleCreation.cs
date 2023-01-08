using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot;

class RoleCreation
{
    /*
    public static bool CheckOneRole(Discord.WebSocket.SocketGuild guild, string roleName)
    {
        var sameNameRoles = guild.Roles.Where(x => x.Name == roleName);
        if (sameNameRoles.Count() == 0)
        {
            Console.WriteLine($"Missing role: {roleName}.");
            return false;
        }

        if (sameNameRoles.Count() > 2)
        {
            throw new GuildStructureException($"The guild {guild.Name} has role {roleName} twice or more. Please fix this outside of the bot.");
        }

        return true;
    }

    public static bool CheckAllRoles(Discord.WebSocket.SocketGuild guild)
    {
        bool allRolesPresent = true;

        foreach (string roleName in RankingV6.ColorMetalRoles)
        {
            bool check = CheckOneRole(guild, roleName);
            if (!check)
            {
                allRolesPresent = false;
            }
        }

        return allRolesPresent;
    }
    /// <summary>
    /// Creates all the roles that the bot needs and which have not been created manually yet.
    /// </summary>
    /// <param name="guild">The guild where we check the roles.</param>
    public static async Task CreateMissingRoles(Discord.WebSocket.SocketGuild guild)
    {
        // First add spectral metal roles, then spectral digit roles.
        foreach (MetalV6 metal in RankingV6.MetalRanks)
        {
            if (metal = MetalV6.Undefined)
            {
                contineu;
            }
            var sameNameRole = guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if (sameNameRole == null)
            {
                System.Console.WriteLine("Creating the missing role " + roleName);
                await guild.CreateRoleAsync(name: roleName, color: RankingV6.MetalColor(), isMentionable: false);
            }
        }

        foreach (string roleName in RankingV5.SpectralDigitRoles)
        {
            var sameNameRole = guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if (sameNameRole == null)
            {
                System.Console.WriteLine("Creating the missing role " + roleName);
                await guild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
            }
        }

        // In the ordering, then come big loud roles and tiny loud roles.
        foreach (string roleName in RankingV5.LoudMetalRoles)
        {
            var sameNameRole = guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if (sameNameRole == null)
            {
                System.Console.WriteLine("Creating the missing role " + roleName);
                await guild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
            }
        }

        foreach (string roleName in RankingV5.LoudDigitRoles)
        {
            var sameNameRole = guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if (sameNameRole == null)
            {
                System.Console.WriteLine("Creating the missing role " + roleName);
                await guild.CreateRoleAsync(name: roleName, color: Settings.roleColor(roleName), isMentionable: false);
            }

        }
    }
    */
}
