using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Reset : UserCommonBase
    {
        public static readonly bool SlashCommand = true;

        public Reset()
        {
            SlashName = "reset";
            SlashDescription = "Smaze vsechny rankove role a vsechny informace o vas z databaze, zacnete 's cistym stitem'.";
        }

        public async override Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            var author = (SocketGuildUser)command.User;

            // Log the command.
            var sourceGuild = Bot.Instance.guilds.byID[author.Guild.Id];
            _ = LogCommand(sourceGuild, author, "/reset");
            await command.DeferAsync(ephemeral: true);

            foreach (DiscordGuild g in Bot.Instance.guilds.byID.Values)
            {
                if (g.IsGuildMember(author.Id))
                {
                    await g.RemoveAllRankRoles(author.Id);
                    await Bot.Instance._data.RemoveFromDatabases(author.Id);

                }
            }

            await command.ModifyOriginalResponseAsync(
                resp => resp.Content = $"Rank resetovan.");
            return;
        }
    }
}
