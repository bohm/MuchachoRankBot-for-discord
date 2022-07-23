using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Update : UserCommonBase
    {
        public Update()
        {
            SlashName = "update";
            SlashDescription = "Bot aktualizuje vas rank na soucasnou hodnotu. I bez prikazu se vas rank aktualizuje kazde 3 hodiny.";
        }

        public static readonly bool SlashCommand = true;
        public override async Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            var author = (SocketGuildUser)command.User;

            // Log the command.
            var sourceGuild = Bot.Instance.guilds.byID[author.Guild.Id];
            _ = LogCommand(sourceGuild, author, "/update");
            await command.DeferAsync(ephemeral: true);

            string authorR6TabId = await Bot.Instance._data.QueryMapping(author.Id);

            if (authorR6TabId == null)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Vas rank nebyl nalezen v databazi tracku.");
                return;
            }

            bool ret = await Bot.Instance.UpdateOne(author.Id);

            if (ret)
            {
                Rank r = await Bot.Instance._data.QueryRank(author.Id);
                // Print user's rank too.
                if (r.met == Metal.Undefined)
                {
                    await command.ModifyOriginalResponseAsync(
                        resp => resp.Content = $"Aktualizace vraci neplatny rank. Asi se neco pokazilo.");
                    return;
                }

                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Update ranku hotov, vas novy rank je {r.FullPrint()}.");
            }
            else
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = "Stala se chyba pri aktualizaci ranku, mate stale predchozi rank.");
                return;
            }
        }
    }
}
