using Discord;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands
{
    /// <summary>
    /// Uses reflection to register every command that is ready as a slash command.
    /// </summary>
    public class CommandManagement
    {
        public static Dictionary<string, UserCommonBase> GuildUserCommandList;
        public static Dictionary<string, AdminCommonBase> GuildAdminCommandList;

        public CommandManagement()
        {
            if(GuildUserCommandList == null)
            {
                GuildUserCommandList = new Dictionary<string, UserCommonBase>();
                BuildCommandList();
            }

            if (GuildAdminCommandList == null)
            {
                GuildAdminCommandList = new Dictionary<string, AdminCommonBase>();
            }

        }

        public void BuildCommandList()
        {
            // Get a list of all commands which are ready
            var assembly = this.GetType().Assembly;
            var commands = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(UserCommonBase)));
            var slashCommands = commands.Where(x => x.GetField("SlashCommand") != null && (bool)x.GetField("SlashCommand").GetValue(null) == true);
            foreach (var commandType in slashCommands)
            {
                UserCommonBase result = (UserCommonBase) Activator.CreateInstance(commandType);
                string name = result.SlashName;
                Console.WriteLine($"Creating an instance for the command {name}.");
                GuildUserCommandList.Add(name, result);
            }
        }

        public async Task SlashCommandHandlerAsync(SocketSlashCommand command)
        {
            if (GuildUserCommandList.Keys.Contains(command.CommandName))
            {
                var commandObject = GuildUserCommandList[command.CommandName];
                await commandObject.ProcessCommandAsync(command);
            }
        }

        public async Task CreateGuildCommandsAsync(DiscordSocketClient socketClient, DiscordGuild targetGuild)
        {
           foreach (UserCommonBase command in GuildUserCommandList.Values)
           {
                SlashCommandBuilder guildCommand = new SlashCommandBuilder();
                Console.WriteLine($"Building the command {command.SlashName} for {targetGuild.GetName()}.");
                guildCommand.WithName(command.SlashName);
                guildCommand.WithDescription(command.SlashDescription);
                foreach (var param in command.ParameterList)
                {
                    guildCommand.AddOption(param.Name, param.Type, param.Description, isRequired: param.IsRequired);
                }

                try
                {
                    await socketClient.Rest.CreateGuildCommand(guildCommand.Build(), targetGuild._socket.Id);;
                }
                catch (HttpException e)
                {
                        Console.WriteLine($"RankBot: Command build error on server {targetGuild.GetName()}: {e.HttpCode}/{e.Message}");
                }
            }
        }
    }
}
