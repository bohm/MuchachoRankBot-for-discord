using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    internal class Louden : CommonBase
    {
        [Command("nahlas")]
        public async Task LoudenCommandAsync()
        {
            if (!await InstanceCheck())
            {
                return;
            }
            // Add the user to any mentionable rank roles.
            var author = Context.Message.Author;

            // Log command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/nahlas");

            await Bot.Instance.LoudenUserAndAddRoles(author.Id);
            await ReplyAsync(author.Username + ": Nyni budete notifikovani, kdyz nekdo zapne vasi roli.");
        }
    }
}
