using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class ShushPlayer : AdminCommonBase
    {
        public static readonly string Name = "!shushplayer discordUsername";
        public static readonly string Description = "Removes tagging roles from a Discord user.";
        [Command("shushplayer")]
        public async Task ShushPlayerCommand(string discordUsername)
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
            await Bot.Instance.QuietenUserAndTakeRoles(rightUser.Id);
            await ReplyAsync($"Discord user {rightUser} is now shushed and won't be pinged.");
        }
    }
}
