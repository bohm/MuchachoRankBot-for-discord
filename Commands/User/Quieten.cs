using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    internal class Quieten : CommonBase
    {
        [Command("ticho")]
        public async Task QuietenCommandAsync()
        {
            if (!await InstanceCheck())
            {
                return;
            }

            var author = Context.Message.Author;

            // Log command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/ticho");

            await Bot.Instance.QuietenUserAndTakeRoles(author.Id);
            await ReplyAsync(author.Username + ": Odted nebudete notifikovani, kdyz nekdo oznaci vasi roli. Poslete prikaz !nahlas pro zapnuti notifikaci.");
        }
    }
}
