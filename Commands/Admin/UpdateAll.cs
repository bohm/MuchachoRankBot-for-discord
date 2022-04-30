using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class UpdateAll : CommonBase
    {
        [Command("updateall")]
        public async Task UpdateAllCommand()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await ReplyAsync("Running a manual update on all users.");
            _ = Bot.Instance.UpdateAll();
        }
    }
}
