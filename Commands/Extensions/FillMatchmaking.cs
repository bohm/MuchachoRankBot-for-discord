using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Extensions
{
    public class FillMatchmaking : UserCommonBase
    {

        public static readonly bool SlashCommand = true;

        public FillMatchmaking()
        {
            SlashName = "fill-matchmaking";
            SlashDescription = "Puts 1-10 people into the matchmaking queue.";
            ParameterList.Add(new CommandParameter("user0", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", true));
            ParameterList.Add(new CommandParameter("user1", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user2", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user3", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user4", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user5", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user6", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user7", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user8", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
            ParameterList.Add(new CommandParameter("user9", Discord.ApplicationCommandOptionType.User, "Discord prezdivka", false));
        }

        public async override Task ProcessCommandAsync(SocketSlashCommand command)
        {
            if (!(await InstanceCheck() && await OperatorCheck(command.User.Id)))
            {
                await command.RespondAsync("You need admin privileges for this command.", ephemeral: true);
                return;
            }
        }
    }
}

