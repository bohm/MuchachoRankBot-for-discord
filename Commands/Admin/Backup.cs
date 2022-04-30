using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    internal class Backup : CommonBase
    {
        [Command("backup")]
        public async Task BackupCommand()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await ReplyAsync("Writing the current state of tracking into rsix.json.");
            _ = Bot.Instance.PerformBackup();
        }
    }
}
