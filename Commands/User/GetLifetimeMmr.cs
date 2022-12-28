using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class GetLifetimeMmr : UserCommonBase
    {

        public static readonly bool SlashCommand = true;

        public GetLifetimeMmr()
        {
            SlashName = "get-lifetime-mmr";
            SlashDescription = "Returns the lifetime MMR for a user.";
            ParameterList.Add(new CommandParameter("user", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", true));
        }

        public async override Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            if (!(await InstanceCheck() && await OperatorCheck(command.User.Id)))
            {
                await command.RespondAsync("You need admin privileges for this command.", ephemeral: true);
                return;
            }

            var author = (SocketGuildUser)command.User;
            DiscordGuild contextGuild = Bot.Instance.Guilds.byID[author.Guild.Id];
            await command.DeferAsync(ephemeral: true);

            var targetUser = (SocketGuildUser)command.Data.Options.First(x => x.Name == "user").Value;

            await LogCommand(contextGuild, author, "/get-lifetime-mmr", $"/get-lifetime-mmr {targetUser.Nickname}");

            // TODO: Insert into the table.
            long r = Bot.Instance.MmrManager.GetMmr(targetUser.Id);
            if (r == -1)
            {
                const string errormsg = "User has no lifetime MMR data (or an error occurred).";
                await command.ModifyOriginalResponseAsync(resp => resp.Content = errormsg);
                return;
            }

            string msg = $"Uzivatel ma {r} lifetime MMR.";
            await command.ModifyOriginalResponseAsync(resp => resp.Content = msg);
        }
    }
}