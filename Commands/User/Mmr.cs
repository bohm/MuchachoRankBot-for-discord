using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Mmr : CommonBase
    {
        [Command("mmr")]
        public async Task MmrCommandAsync(string discordNick)
        {
            if (!await InstanceCheck())
            {
                return;
            }
            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            var target = contextGuild.GetSingleUser(discordNick);

            // Log the command.
            await LogCommand(contextGuild, author, "/mmr", $"/mmr {discordNick}");

            if (target == null)
            {
                await ReplyAsync(author.Username + ": Nenasli jsme cloveka ani podle prezdivky, ani podle Discord jmena.");
                return;
            }

            if (Bot.Instance._data.DiscordUplay.ContainsKey(target.Id))
            {
                string uplayId = Bot.Instance._data.DiscordUplay[target.Id];
                int mmr = -1;
                try
                {
                    mmr = await Bot.Instance.uApi.GetMMR(uplayId);
                    if (mmr < 0)
                    {
                        throw new RankParsingException("Returned MMR is less than 0, that is almost surely wrong.");
                    }
                }
                catch (Exception r)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se ziskat MMR z trackeru.");
                    await ReplyAsync("Admin info: " + r.Message);
                    return;
                }
                await ReplyAsync($"{author.Username}: Clen {discordNick} ma {mmr} MMR.");
                return;
            }
            else
            {
                await ReplyAsync(author.Username + ": Discord uzivatel " + discordNick + " existuje, ale nenasli jsme ho v databazi ranku.");
                return;
            }
        }


    }
}
