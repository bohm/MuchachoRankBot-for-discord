using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RankBot
{
    public class CommonBase : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Checks if bot has the instance set up. Otherwise, commands will not run.
        /// </summary>
        /// <returns>true if instance exists, false otherwise.</returns>
        protected async Task<bool> InstanceCheck()
        {
            if (Bot.Instance == null)
            {
                await ReplyAsync("R6RankBot has no set server as a residence, it cannot proceed.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for operator privileges, returns false otherwise.
        /// </summary>
        /// <returns></returns>

        protected async Task<bool> OperatorCheck(ulong id)
        {
            if (!Settings.Operators.Contains(id))
            {
                await ReplyAsync("This command needs operator privileges.");
                return false;
            }
            return true;
        }
    }
}
