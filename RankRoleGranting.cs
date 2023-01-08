using RankBot.Commands.Admin;
using RankBot.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot;


public class RoleInformation
{
    public string Name;
    public string Color;
    public ulong GuildRoleId;
    public MetalV6 CorrespondingMetal;
}


public class GuildRoleData
{
    public ulong GuildId;
    public List<RoleInformation> ManagedRoles;
}

public class RoleDataStructure
{
    public Dictionary<ulong, GuildRoleData> Guilds;

    public RoleDataStructure()
    {
        Guilds = new Dictionary<ulong, GuildRoleData>();
    }
}

public class RankRoleGuildManager
{
    private DiscordGuild _guild;
    private GuildRoleData _roleData;
    private List<ulong> _managedRoleIds;

    public RankRoleGuildManager(DiscordGuild guild, GuildRoleData data)
    {
        _guild = guild;
        _roleData = data;
        _managedRoleIds = new List<ulong>();

        // Populate the _managedRoleIds list, for easier clearing of all roles.
        foreach (var x in _roleData.ManagedRoles)
        {
            _managedRoleIds.Add(x.GuildRoleId);
        }
    }

    public RoleInformation RoleFromMetal(MetalV6 met)
    {
        return _roleData.ManagedRoles.FirstOrDefault(x => x.CorrespondingMetal == met);
    }

    public bool IsGuildMember(ulong userId)
    {
        return _guild.IsGuildMember(userId);
    }

    /// <summary>
    /// Updates a single user to a new metal role. Currently a very API-intense
    /// implementation.
    /// </summary>
    /// <param name="userId">Discord user's ID.</param>
    /// <param name="metalToSet">New role to be set.</param>
    /// <returns></returns>
    public async Task UpdateUser(ulong userId, MetalV6 metalToSet)
    {

        await _guild.RemoveRolesAsync(userId, _managedRoleIds);
        await _guild.AddRoleAsync(userId, RoleFromMetal(metalToSet).GuildRoleId);
    }
}


public class RankRoleManager
{
    private BackupSystem<RoleDataStructure> _backup;
    private const string _backupChannelName = "rank-role-management";
    private const string _backupFileName = "rank-role-management.json";

    private RoleDataStructure _allGuildData;
    private PrimaryDiscordGuild _pg;
    private Dictionary<ulong, RankRoleGuildManager> _submanagers;

    public RankRoleManager()
    {
        _submanagers = new Dictionary<ulong, RankRoleGuildManager>();
    }


    public async Task DelayedInit(PrimaryDiscordGuild primg, DiscordGuilds guilds)
    {
        _pg = primg;

        // Check if the primary discord guild contains the channel and if not, assume we run it for the first time.

        if (!_pg._socket.TextChannels.Any(x => x.Name == _backupChannelName))
        {
            _allGuildData = new RoleDataStructure();
            _backup = await BackupSystem<RoleDataStructure>.NewBackupSystemAsync(_backupChannelName, _backupFileName, _allGuildData, _pg);
            Console.WriteLine("Created a new backup system for rank role mapping.");
        }
        else
        {
            _backup = new BackupSystem<RoleDataStructure>(_pg, _backupChannelName, _backupFileName);
            _allGuildData = await _backup.RecoverAsync();
            Console.WriteLine($"Initialized rank role management for {_allGuildData.Guilds.Count} Discord guilds.");
        }

        // Initialize sub-managers.

        foreach (var (guildId,guildInfo) in _allGuildData.Guilds)
        {
            DiscordGuild g = guilds.byID[guildId];
            RankRoleGuildManager subm = new RankRoleGuildManager(g, guildInfo);
            _submanagers.Add(guildId, subm);
        }
    }

    /// <summary>
    /// Updates a user in all managed Discord guilds.
    /// </summary>
    /// <param name="userId">Discord user's ID.</param>
    /// <param name="metalToSet">The new Metal role to be granted.</param>
    /// <returns></returns>
    public async Task UpdateUser(ulong userId, MetalV6 metalToSet)
    {
        foreach (var (guildId, subm) in _submanagers)
        {
            if (subm.IsGuildMember(userId))
            {
                await subm.UpdateUser(userId, metalToSet);
            }
        }
    }
}