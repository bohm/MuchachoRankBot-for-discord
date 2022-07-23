using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class UplayName : UserCommonBase
    {

        public UplayName()
        {
            SlashName = "uplay";
            SlashDescription = "Vypise Uplay ucet daneho cloveka (napr. abyste si ho mohli pridat).";
            ParameterList.Add(new CommandParameter("user", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", true));
        }
        
        public static readonly bool SlashCommand = true;


        public override async Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            var author = (SocketGuildUser) command.User;

            DiscordGuild contextGuild = Bot.Instance.guilds.byID[author.Guild.Id];

            await command.DeferAsync(ephemeral: true);

            var targetUser = (SocketGuildUser) command.Data.Options.First(x => x.Name == "user").Value;

            if (targetUser == null)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = "Chybi parameter 'user', nemuzeme pokracovat.");
                return;
            }

            // Log the command.
            await LogCommand(contextGuild, author, "/uplay", $"/uplay {targetUser.Username}");

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
                string uplayId = Bot.Instance._data.DiscordUplay[targetUser.Id];
                // This is only the uplay id, the user name might be different. We have to query a tracker to get the current
                // Uplay user name.
                string uplayName = "";
                try
                {
                    uplayName = await Bot.Instance.uApi.GetUplayName(uplayId);
                    if (uplayName == null || uplayName.Length == 0)
                    {
                        await command.ModifyOriginalResponseAsync(
                                resp => resp.Content = $"Nemohli jsme ukol dokoncit, problem komunikace s trackerem.");
                        throw new RankParsingException("Returned uplayName is empty.");
                    }
                }
                catch (Exception)
                {
                    await command.ModifyOriginalResponseAsync(
                        resp => resp.Content = $"Nemohli jsme ukol dokoncit, problem komunikace s trackerem.");
                    return;
                }
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Clen {targetUser.Username} se na Uplayi jmenuje \"{uplayName}\".");
                return;
            }
        }
    }
}
