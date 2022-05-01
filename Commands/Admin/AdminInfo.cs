using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class AdminInfo : AdminCommonBase
    {
        public static readonly string Name = "!admincommands";
        public static readonly string Description = "Lists all administrative commands";
        [Command("admincommands")]
        public async Task AdminInfoCommandAsync()
        {
            // Uses reflection to get all commands defined as descendants of CommonBase, and lists their name and description.
            StringBuilder advancedReply = new StringBuilder();
            var assembly = this.GetType().Assembly;
            var commands = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(AdminCommonBase)));
            foreach (var commandType in commands)
            {
                var nameField = commandType.GetField("Name");
                var descriptionField = commandType.GetField("Description");

                if (nameField != null && descriptionField != null)
                {
                    string commandName = (string)nameField.GetValue(null);
                    string commandDescription = (string)descriptionField.GetValue(null);
                    advancedReply.Append(commandName);
                    advancedReply.Append(" -- ");
                    advancedReply.Append(commandDescription);
                    advancedReply.Append("\n");
                }
            }
            await ReplyAsync(advancedReply.ToString());
        }
    }
}