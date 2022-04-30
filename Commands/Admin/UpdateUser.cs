using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class UpdateUser : CommonBase
    {
        // --- Admin commands. ---

        [Command("updateuser")]
        public async Task UpdateUserCommand(string discordUsername)
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

            try
            {
                if (!await Bot.Instance._data.UserTracked(target.Id))
                {
                    await ReplyAsync($"User {target.Username} not tracked.");
                    return;

                }

                bool ret = await Bot.Instance.UpdateOne(target.Id);

                if (ret)
                {
                    await ReplyAsync($"User {target.Username} updated.");
                    // Print user's rank too.
                    Rank r = await Bot.Instance._data.QueryRank(target.Id);
                    if (r.Digits())
                    {
                        await ReplyAsync($"We see {target.Username}'s rank as {r.FullPrint()}");
                    }
                    else
                    {
                        await ReplyAsync($"We see {target.Username}'s rank as {r.CompactFullPrint()}");
                    }
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
