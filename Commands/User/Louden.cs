using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Louden : UserCommonBase
    {
        public Louden()
        {
            SlashName = "nahlas";
            SlashDescription = "Bot vam zapne zpet pingovatelne role, pokud jste si je predtim vypli.";
        }

        public static readonly bool SlashCommand = true;

        public override async Task ProcessCommandAsync(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            // Add the user to any mentionable rank roles.
            var author = command.User as SocketGuildUser;

            // Log command.
            var sourceGuild = Bot.Instance.Guilds.byID[author.Guild.Id];
            _ = LogCommand(sourceGuild, author, "/nahlas");

            await Bot.Instance.LoudenUserAndAddRoles(author.Id);
            await command.ModifyOriginalResponseAsync(
                resp => resp.Content = "Nyni budete notifikovani, kdyz nekdo zapne vasi roli.");
        }
    }
}
