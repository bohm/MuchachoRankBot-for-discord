using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    internal class MyRank : CommonBase
    {
        [Command("rank")]
        public async Task Rank()
        {
            if (!await InstanceCheck())
            {
                return;
            }

            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            string authorR6TabId = await Bot.Instance._data.QueryMapping(author.Id);

            // Log the command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/rank");

            if (authorR6TabId == null)
            {
                await ReplyAsync(author.Username + ": vas rank netrackujeme, tak nemuzeme slouzit.");
                return;
            }
            else
            {
                try
                {

                    Rank r = await Bot.Instance.uApi.GetRank(authorR6TabId);
                    if (r.Digits())
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.FullPrint());
                    }
                    else
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.CompactFullPrint());
                    }
                }
                catch (RankParsingException)
                {
                    await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
                }
            }
        }
    }
}
