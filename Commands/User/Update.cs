using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Update : CommonBase
    {
        [Command("update")]
        public async Task UpdateCommandAsync()
        {
            if (!await InstanceCheck())
            {
                return;
            }

            try
            {
                var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

                // Log the command.
                var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
                await LogCommand(sourceGuild, author, "/update");

                string authorR6TabId = await Bot.Instance._data.QueryMapping(author.Id);

                if (authorR6TabId == null)
                {
                    await ReplyAsync(author.Username + ": vas rank netrackujeme, tak nemuzeme slouzit.");
                    return;
                }

                bool ret = await Bot.Instance.UpdateOne(author.Id);

                if (ret)
                {
                    await ReplyAsync(author.Username + ": Aktualizovali jsme vase MMR a rank. Nezapomente, ze to jde jen jednou za 30 minut.");
                    // Print user's rank too.
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
                else
                {
                    await ReplyAsync(author.Username + ": Stala se chyba pri aktualizaci ranku, mate stale predchozi rank.");
                }

            }
            catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the local Discord admins.");
                return;
            }
        }
    }
}
