using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class Populate : AdminCommonBase
    {
        public static readonly string Name = "!populate";
        public static readonly string Description = "Creates the required roles for the bot to work. Necessary before any tracking can start.";

        [Command("populate")]
        public async Task PopulateCommand()
        {
       
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            // await RoleCreation.CreateMissingRoles(Context.Guild);
        }
    }
}
