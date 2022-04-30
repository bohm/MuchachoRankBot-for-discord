using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Track : CommonBase
    {
        [Command("track")]
        public async Task TrackCommandAsync(string nick)
        {
            if (!await InstanceCheck())
            {
                return;
            }

            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            // Log the command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/track", $"/track {nick}");

            try
            {
                string queryR6ID = await Bot.Instance._data.QueryMapping(author.Id);
                if (queryR6ID != null)
                {
                    await ReplyAsync(author.Username + ": Vas discord ucet uz sledujeme. / We are already tracking your Discord account.");
                    return;
                }

                string r6TabId = await Bot.Instance.uApi.GetID(nick);

                if (r6TabId == null)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.");
                    return;
                }
                await Bot.Instance._data.InsertIntoMapping(author.Id, r6TabId);
                await ReplyAsync(author.Username + ": nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme PC. / We now track you as " + nick + " on PC.");

                // Update the newly added user.

                bool ret = await Bot.Instance.UpdateOne(author.Id);

                if (ret)
                {
                    // Print user's rank too.
                    Rank r = await Bot.Instance.uApi.GetRank(r6TabId);
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
                    await ReplyAsync(author.Username + ": Stala se chyba pri nastaven noveho ranku.");
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
