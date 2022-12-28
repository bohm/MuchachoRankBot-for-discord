using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class TrackPlayer : AdminCommonBase
    {
        public static readonly string Name = "!trackuser discordUsername uplayNick";
        public static readonly string Description = "Starts tracking a specific user. Equivalent to !track.";
        [Command("trackplayer")]
        public async Task TrackPlayerCommand(string discordUsername, string nick)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            DiscordGuild guild = Bot.Instance.Guilds.byID[Context.Guild.Id];
            var matchedUsers = guild.GetAllUsers(discordUsername);

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

            if (rightUser != null)
            {
                // We do not await this one, no need -- it should respond on its own time.
                _ = Bot.Instance.TrackUser(guild, rightUser.Id, nick, Context.Message.Channel.Name);
            }
        }
    }
}
