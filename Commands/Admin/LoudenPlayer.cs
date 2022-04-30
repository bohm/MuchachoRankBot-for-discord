using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    internal class LoudenPlayer : CommonBase
    {
        [Command("loudenplayer")]
        public async Task LoudenPlayerCommand(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            var matchedUsers = Context.Guild.Users.Where(x => x.Username.Equals(discordUsername) || x.Nickname.Equals(discordUsername));

            if (matchedUsers.Count() == 0)
            {
                await ReplyAsync("There is no user matching the Discord nickname " + discordUsername + ".");
                return;
            }

            if (matchedUsers.Count() > 1)
            {
                await ReplyAsync("Two or more users have the same matching Discord nickname. This command cannot continue.");
                return;
            }

            Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();
            // Add the user to any mentionable rank roles.
            await Bot.Instance.LoudenUserAndAddRoles(rightUser.Id);
            await ReplyAsync($"Discord user {rightUser.Username} is now set to loud.");
        }
    }
}