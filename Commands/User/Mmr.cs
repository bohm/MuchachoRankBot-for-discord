using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Mmr : UserCommonBase
    {

        public Mmr()
        {
            SlashName = "mmr";
            SlashDescription = "Vypise aktualni MMR hrace (napr. abyste vedeli, jestli se s nim vejdete).";
            ParameterList.Add(new CommandParameter("user", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", true));
        }

        public static readonly bool SlashCommand = true;


        public override async Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            var author = (SocketGuildUser)command.User;

            DiscordGuild contextGuild = Bot.Instance.guilds.byID[author.Guild.Id];

            await command.DeferAsync(ephemeral: true);

            var targetUser = (SocketGuildUser)command.Data.Options.First(x => x.Name == "user").Value;

            if (targetUser == null)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = "Chybi parameter 'user', nemuzeme pokracovat.");
                return;
            }

            var target = contextGuild.GetSingleUser(targetUser.Id);
            if (target == null)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = "Nenalezli jsme uzivatele na Discordu, nemuzeme pokracovat.");
                return;
            }

            if (!Bot.Instance._data.DiscordUplay.ContainsKey(targetUser.Id))
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Discord uzivatel {targetUser.Username} nenalezen v databazi tracku.");
                return;
            }
            else
            {
                string uplayId = Bot.Instance._data.DiscordUplay[target.Id];
                int mmr = -1;
                try
                {
                    mmr = await Bot.Instance.uApi.GetMMR(uplayId);
                    if (mmr < 0)
                    {
                        await command.ModifyOriginalResponseAsync(
                            resp => resp.Content = $"Nemohli jsme ukol dokoncit, problem komunikace s trackerem.");
                        throw new RankParsingException("Returned MMR is less than 0, that is almost surely wrong.");
                    }
                }
                catch (Exception r)
                {
                    await command.ModifyOriginalResponseAsync(
                        resp => resp.Content = $"Nemohli jsme ukol dokoncit, problem komunikace s trackerem.");
                    return;
                }

                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Clen {targetUser.Username} ma {mmr} MMR.");
                return;
            }
        }
    }
}
