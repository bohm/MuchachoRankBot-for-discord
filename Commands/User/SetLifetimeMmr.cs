using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class SetLifetimeMmr : UserCommonBase
    {

        public static readonly bool SlashCommand = true;

        public SetLifetimeMmr()
        {
            SlashName = "set-lifetime-mmr";
            SlashDescription = "Sets the lifetime MMR for a user.";
            ParameterList.Add(new CommandParameter("user", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", true));
            ParameterList.Add(new CommandParameter("mmr", Discord.ApplicationCommandOptionType.Integer, "Lifetime MMR", true)); 
        }

        public async override Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            if (!(await InstanceCheck() && await OperatorCheck(command.User.Id)))
            {
                await command.RespondAsync("You need admin privileges for this command.", ephemeral: true);
                return;
            }

            var author = (SocketGuildUser)command.User;
            DiscordGuild contextGuild = Bot.Instance.guilds.byID[author.Guild.Id];
            await command.DeferAsync(ephemeral: true);

            var targetUser = (SocketGuildUser) command.Data.Options.First(x => x.Name == "user").Value;
            long MMR = (long) command.Data.Options.First(x => x.Name == "mmr").Value;

            await LogCommand(contextGuild, author, "/set-lifetime-mmr", $"/set-lifetime-mmr {targetUser.Nickname} {MMR}");

            // TODO: Insert into the table.
            await Bot.Instance.MmrManager.SetMmr(targetUser.Id, MMR);

            const string msg = "Polozka vlozena.";
            await command.ModifyOriginalResponseAsync(resp => resp.Content = msg);
        }
    }
}
