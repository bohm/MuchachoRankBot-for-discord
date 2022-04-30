using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    internal class Populate : CommonBase
    {
        [Command("populate")]
        public async Task PopulateCommand()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await RoleCreation.CreateMissingRoles(Context.Guild);
        }
    }
}
