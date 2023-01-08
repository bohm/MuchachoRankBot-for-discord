using System.Collections.Generic;
using System.Threading.Tasks;

namespace RankBot.Extensions;

/*
public class ActiveRoleGranting
{
    private DiscordGuild _targetGuild;
    private BotDataStructure _rankData;
    private ActiveUserManagement _actMgr;
    private bool _initComplete = false;

    private HashSet<ulong> _allActiveRoles;
    private Dictionary<Metal, ulong> _activeRoleMapping;

    ActiveRoleGranting()
    {
        
    }

    public async Task DelayedInit(DiscordGuild dg, BotDataStructure rankData, ActiveUserManagement actMgr)
    {
        _targetGuild = dg;
        _rankData = rankData;
        _initComplete = true;
        _actMgr = actMgr;
    }

    public async HashSet<ulong> AssignedActiveRoles(ulong userId)
    {
        HashSet<ulong> allRoles = await _targetGuild.AllUserRoles(userId);
        allRoles.IntersectWith(_allActiveRoles);
        return allRoles;
    }

    public async HashSet<ulong> ExpectedActiveRoles(ulong userId)
    {
        bool userActive = _actMgr.QueryActiveness(userId);
        if (!userActive)
        {
            return new HashSet<ulong>();
        }
        else
        {
            int rp = await _rankData.QueryRP(userId);
            if (rp <= 0)
            {
                return new HashSet<ulong>();
            }
            
            Metal m = MetalFromRP(rp);
            if (m == Metal.Undefined)
            {
                return new HashSet<ulong>();
            }

            ulong expectedRole = _activeRoleMapping[m];
            return new HashSet<ulong>{expectedRole};
        }
    }
    
    /// <summary>
    /// The regular update goes through all active players, and if they are members
    /// of the guild, it grants them the right roles.
    /// </summary>
    public async Task QuickUpdate()
    {
        if (!_initComplete)
        {
            return;
        }
        
        
    }
    
    /// <summary>
    /// The full update goes through all guild users, checks their activity and
    /// adds/removes roles accordingly.
    /// </summary>
    public async Task FullUpdate()
    {
        if (!_initComplete)
        {
            return;
        }
    }
}
*/