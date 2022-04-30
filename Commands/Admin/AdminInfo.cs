using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class AdminInfo : CommonBase
    {
        [Command("admincommands")]
        public async Task AdminInfoCommand()
        {
            await ReplyAsync(@"Available admin commands:
!populate -- creates the required roles for the bot to work. Necessary before any tracking can start.
!resetuser ID -- clears the ranks of a specific user. Equivalent to !reset. Needs discord ID (the ulong) as parameter.
!updateuser discordUsername -- updates the rank of a specific user. Equivalent to !update.
!updateall -- triggers the update of all people at the Discord server, which normally runs periodically.
!backup -- backs up the current memory of the bot into the Discord message and into the secondary json backup.
!manualrank discordUsername spectralRankName -- Sets a rank without querying anything. Useful for debugging or a quick correction.
!trackuser discordUsername uplayNick -- starts tracking a specific user. Equivalent to !track.");
        }
    }
}