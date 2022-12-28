using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RankBot.Extensions;

public class ActiveUserField
{
    public bool Active = false;
    public DateTime ActiveDate;

    public void Activate()
    {
        Active = true;
        ActiveDate = DateTime.Now;
    }
}

public class ActiveDataStructure
{
    public Dictionary<ulong, ActiveUserField> DiscordUsers;

    public ActiveDataStructure()
    {
        DiscordUsers = new Dictionary<ulong, ActiveUserField>();
    }
}

public class ActiveUserManagement
{
    private PrimaryDiscordGuild _pg;
    private ActiveDataStructure _ads;
    private BackupSystem<ActiveDataStructure> _backup;
    private const string _backupChannelName = "active-user-tracking";
    private const string _backupFileName = "active-user-tracking.json";

    ActiveUserManagement()
    {

    }

    public async Task DelayedInit(PrimaryDiscordGuild primg)
    {
        _pg = primg;

        // Check if the primary discord guild contains the channel and if not, assume we run it for the first time.

        if (_pg._socket.TextChannels.Any(x => x.Name == _backupChannelName))
        {
            _backup = new BackupSystem<ActiveDataStructure>(_pg, _backupChannelName, _backupFileName);
            _ads = await _backup.RecoverAsync();
            Console.WriteLine($"Initialized {_ads.DiscordUsers.Count} elements from the active user backup.");
        }
        else
        {
            _ads = new ActiveDataStructure();
            _backup = await BackupSystem<ActiveDataStructure>.NewBackupSystemAsync(
                _backupChannelName, _backupFileName, _ads, _pg);
            Console.WriteLine("Created a new backup system for active user tracking.");
        }
    }

    public bool QueryActiveness(ulong discordId)
    {
        if (!_ads.DiscordUsers.ContainsKey(discordId))
        {
            return false;
        }

        bool ret = _ads.DiscordUsers[discordId].Active;
        return ret;
    }

    public bool QueryActiveness(SocketGuildUser user)
    {
        return QueryActiveness(user.Id);
    }

    public async Task SetActive(ulong discordId)
    {
        ActiveUserField a = new ActiveUserField();
        a.Activate();
        _ads.DiscordUsers[discordId] = a;
        await _backup.BackupAsync(_ads);
    }

    public async Task SetActive(SocketGuildUser user)
    {
        await SetActive(user.Id);
    }

    public async Task SetInactive(ulong discordId)
    {
        ActiveUserField a = new ActiveUserField();
        _ads.DiscordUsers[discordId] = a;
        await _backup.BackupAsync(_ads);
    }

    public async Task SetInactive(SocketGuildUser user)
    {
        await SetInactive(user.Id);
    }
}