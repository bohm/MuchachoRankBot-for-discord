using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class ResetUser : AdminCommonBase
    {
        public static readonly string Name = "!resetuser discordUsername";
        public static readonly string Description = "Clears the ranks of a specific user. Equivalent to !reset. Needs a discord username as a parameter.";

        [Command("resetuser")]
        public async Task ResetUserCommand(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            Discord.WebSocket.SocketGuildUser target = contextGuild.GetSingleUser(discordUsername);

            if (target == null)
            {
                await ReplyAsync("The provided ID does not match any actual user.");
                return;
            }
            else
            {
                await ReplyAsync($"Erasing all roles and ranks from the user {target.Username} from all guilds. ");
                foreach (var guild in Bot.Instance.guilds.guildList)
                {
                    if (guild.IsGuildMember(target.Id))
                    {
                        await guild.RemoveAllRankRoles(target);
                    }
                }

                await Bot.Instance._data.RemoveFromDatabases(target.Id);
            }
        }
    }
}
