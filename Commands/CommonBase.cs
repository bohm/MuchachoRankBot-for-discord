using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace RankBot
{
    public class CommonBase : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Checks if bot has the instance set up. Otherwise, commands will not run.
        /// </summary>
        /// <returns>true if instance exists, false otherwise.</returns>
        protected async Task<bool> InstanceCheck()
        {
            if (Bot.Instance == null || !Bot.Instance.ConstructionComplete)
            {
                await ReplyAsync("RankBot is not yet ready to process commands.");

                return false;
            }

            return true;
        }

        protected async Task<bool> InstanceCheck(SocketSlashCommand command)
        {
            if (Bot.Instance == null || !Bot.Instance.ConstructionComplete)
            {
                await command.RespondAsync("The Discord server is not yet ready for commands.", ephemeral: true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for operator privileges, returns false otherwise.
        /// </summary>
        /// <returns></returns>

        protected async Task<bool> OperatorCheck(ulong id)
        {
            if (!Settings.Operators.Contains(id))
            {
                await ReplyAsync("This command needs operator privileges.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Builds a log message and posts it to the appropriate channel.
        /// </summary>
        /// <param name="g">Discord guild where the action occurred.</param>
        /// <param name="user">A SocketUser which initiated the interaction.</param>
        /// <param name="commandName">Name of the command being launched.</param>
        /// <param name="details">Any other relevant details.</param>
        /// <returns></returns>
        protected async Task LogCommand(DiscordGuild g, Discord.WebSocket.SocketUser user, string commandName, string details = "")
        {
            if(!Settings.Logging)
            {
                return;
            }

            if(g.Config.loggingChannel == null)
            {
                return;
            }

            // Grab the logging channel.

            Discord.WebSocket.SocketTextChannel logChan = g._socket.TextChannels.First(x => x.Name == g.Config.loggingChannel);

            if (logChan == null)
            {
                return;
            }

            string logString = $"{DateTime.Now.ToString("s")}: {commandName} initiated by the user {user.Username} (ID: {user.Id})";

            if (details.Length > 0)
            {
                logString += $". Full command: {details}.";
            } else
            {
                logString += ".";
            }

            await logChan.SendMessageAsync(logString);
        }

        protected async Task LogError(DiscordGuild g, SocketUser user, string commandName, string errorMessage)
        {
            if (!Settings.Logging)
            {
                return;
            }

            if (g.Config.loggingChannel == null)
            {
                return;
            }

            // Grab the logging channel.

            Discord.WebSocket.SocketTextChannel logChan = g._socket.TextChannels.First(x => x.Name == g.Config.loggingChannel);

            if (logChan == null)
            {
                return;
            }

            string logString = $"{DateTime.Now.ToString("s")}: {commandName} failed for the user {user.Username} (ID: {user.Id}): {errorMessage}.";
            await logChan.SendMessageAsync(logString);
        }
    }

    /// <summary>
    /// A common base for admin commands. Currently used for reflection purposes only.
    /// </summary>
    public class AdminCommonBase : CommonBase
    {

    }

    /// <summary>
    /// A wrapper to collect data about command parameters before their generation.
    /// </summary>
    public class CommandParameter
    {
        public string Name;
        public Discord.ApplicationCommandOptionType Type;
        public string Description;
        public bool IsRequired;

        public CommandParameter(string name, ApplicationCommandOptionType type, string desc, bool isRequired = false)
        {
            Name = name;
            Type = type;
            Description = desc;
            IsRequired = isRequired;
        }
    }

    /// <summary>
    /// A common abstract base for user commands.
    /// </summary>
    public abstract class UserCommonBase : CommonBase
    {
        public string SlashName;
        public string SlashDescription;
        public List<CommandParameter> ParameterList = new();
        public abstract Task ProcessCommandAsync(SocketSlashCommand command);
    }

}
