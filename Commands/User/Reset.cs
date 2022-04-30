using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    internal class Reset : CommonBase
    {
        [Command("reset")]
        public async Task ResetCommandAsync()
        {
            if (!await InstanceCheck())
            {
                return;
            }

            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/reset");

            foreach (DiscordGuild g in Bot.Instance.guilds.byID.Values)
            {
                if (g.IsGuildMember(author.Id))
                {
                    await g.RemoveAllRankRoles(author.Id);
                    await Bot.Instance._data.RemoveFromDatabases(author.Id);
                    await ReplyAsync(author.Username + ": Smazali jsme o vas vsechny informace. Muzete se nechat znovu trackovat.");
                }
            }
        }
    }
}
