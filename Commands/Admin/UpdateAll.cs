using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class UpdateAll : AdminCommonBase
    {
        public static readonly string Name = "!updateall";
        public static readonly string Description = "Triggers the update of all people at the Discord server, which normally runs periodically.";
        [Command("updateall")]
        public async Task UpdateAllCommandAsync()
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
