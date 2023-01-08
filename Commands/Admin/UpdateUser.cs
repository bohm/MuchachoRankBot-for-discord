using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class UpdateUser : AdminCommonBase
    {
        public static readonly string Name = "!updateuser discordUsername";
        public static readonly string Description = "Updates the rank of a specific user. Equivalent to !update.";

        [Command("updateuser")]
        public async Task UpdateUserCommandAsync(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            DiscordGuild contextGuild = Bot.Instance.Guilds.byID[Context.Guild.Id];
            Discord.WebSocket.SocketGuildUser target = contextGuild.GetSingleUser(discordUsername);

            if (target == null)
            {
                await ReplyAsync("The provided ID does not match any actual user.");
                return;
            }

            try
            {
                if (!await Bot.Instance.Data.UserTracked(target.Id))
                {
                    await ReplyAsync($"User {target.Username} not tracked.");
                    return;

                }

                bool ret = await Bot.Instance.UpdateOne(target.Id);

                if (ret)
                {
                    await ReplyAsync($"User {target.Username} updated.");
                    // Print user's rank too.
                    RankDataPointV6 dataPoint = await Bot.Instance.Data.QueryRankInfo(target.Id);
                    await ReplyAsync($"We see {target.Username}'s rank as {RankingV6.MetalPrint(dataPoint.ToMetal())}");
                }
                else
                {
                    await ReplyAsync("Error during rank update (ret is false).");
                    return;
                }

            }
            catch (RankParsingException)
            {
                await ReplyAsync("Error during rank update (RankParsingException).");
                return;
            }
        }
    }
}
