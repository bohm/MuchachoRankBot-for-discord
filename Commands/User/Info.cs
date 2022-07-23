using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Info : UserCommonBase
    {
        public Info()
        {
              SlashName = "prikazy";
              SlashDescription = "Vypise vsechny uzitecne prikazy.";
        }

        public static readonly bool SlashCommand = true;

        public int CommandPriority(string name)
        {
            if (name == "track")
            {
                return 2;
            }

            if (name == "update")
            {
                return 1;
            }

            return 0;
        }

        public override async Task ProcessCommandAsync(SocketSlashCommand command)
        {

            // Uses reflection to get all commands defined as descendants of CommonBase, and lists their name and description.
            StringBuilder advancedReply = new StringBuilder();
            advancedReply.AppendLine("Uzitecne prikazy:");
            // Handle slash commands listing.
            var slashUserGuildCommands = CommandManagement.GuildUserCommandList.Values;
            var sortedUserGuildCommands = slashUserGuildCommands.OrderByDescending(command => CommandPriority(command.SlashName));
            foreach(var userGuildCommand in sortedUserGuildCommands)
            {
                advancedReply.Append("/" + userGuildCommand.SlashName);
                advancedReply.Append(" -- ");
                advancedReply.Append(userGuildCommand.SlashDescription);
                advancedReply.Append("\n");
            }

            await command.RespondAsync(advancedReply.ToString(), ephemeral: true);
        }
    }
}
