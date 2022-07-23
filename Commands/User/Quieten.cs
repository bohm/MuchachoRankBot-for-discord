using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Quieten : UserCommonBase
    {
        public Quieten()
        {
            SlashName = "ticho";
            SlashDescription = "Bot vam odstrani role, ktere jdou pingovat v mistnosti #hledame-spoluhrace.";
        }

        public static readonly bool SlashCommand = true;

        public override async Task ProcessCommandAsync(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            // Add the user to any mentionable rank roles.
            var author = command.User as SocketGuildUser;

            // Log command.
            var sourceGuild = Bot.Instance.guilds.byID[author.Guild.Id];
            _ = LogCommand(sourceGuild, author, "/ticho");

            await Bot.Instance.QuietenUserAndTakeRoles(author.Id);
            await command.ModifyOriginalResponseAsync(
                resp => resp.Content = "Odted nebudete notifikovani, kdyz nekdo oznaci vasi roli. Prikazem /nahlas to muzete vratit zpet.");
        }
    }
}
