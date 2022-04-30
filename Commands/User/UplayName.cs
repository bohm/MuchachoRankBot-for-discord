using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    internal class UplayName : CommonBase
    {

        [Command("uplay")]
        public async Task UplayNameCommandAsync(string discordNick)
        {
            if (!await InstanceCheck())
            {
                return;
            }
            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];

            // Log the command.
            await LogCommand(contextGuild, author, "/uplay", $"/uplay {discordNick}");

            var target = contextGuild.GetSingleUser(discordNick);
            if (target == null)
            {
                await ReplyAsync(author.Username + ": Nenasli jsme cloveka ani podle prezdivky, ani podle Discord jmena.");
                return;
            }

            if (Bot.Instance._data.DiscordUplay.ContainsKey(target.Id))
            {
                string uplayId = Bot.Instance._data.DiscordUplay[target.Id];
                // This is only the uplay id, the user name might be different. We have to query a tracker to get the current
                // Uplay user name.
                string uplayName = "";
                try
                {
                    uplayName = await Bot.Instance.uApi.GetUplayName(uplayId);
                    if (uplayName == null || uplayName.Length == 0)
                    {
                        throw new RankParsingException("Returned uplayName is empty.");
                    }
                }
                catch (Exception r)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se ziskat prezdivku z trackeru.");
                    await ReplyAsync("Admin info: " + r.Message);
                    return;
                }
                await ReplyAsync($"{author.Username}: Clen {discordNick} se na Uplayi jmenuje \"{uplayName}\".");
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
