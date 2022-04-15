﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

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
            if (Bot.Instance == null || !Bot.Instance.constructionComplete)
            {
                await ReplyAsync("RankBot is not yet ready to process commands.");

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
    }
}
