﻿using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RankBot.Commands
{
    public class BasicAdmin : CommonBase
    {
        [Command("admininfo")]
        public async Task AdminInfo()
        {
            await ReplyAsync(@"Available admin commands:
!populate -- creates the required roles for the bot to work. Necessary before any tracking can start.
!resetuser ID -- clears the ranks of a specific user. Equivalent to !reset. Needs discord ID (the ulong) as parameter.
!updateuser discordUsername -- updates the rank of a specific user. Equivalent to !update.
!updateall -- triggers the update of all people at the Discord server, which normally runs periodically.
!backup -- backs up the current memory of the bot into the Discord message and into the secondary json backup.
!manualrank discordUsername spectralRankName -- Sets a rank without querying anything. Useful for debugging or a quick correction.
!trackuser discordUsername uplayNick -- starts tracking a specific user. Equivalent to !track.");
        }

        [Command("populate")]
        public async Task Populate()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await RoleCreation.CreateMissingRoles(Context.Guild);
        }

        [Command("resetuser")]
        public async Task ResetUser(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            Discord.WebSocket.SocketGuildUser target = contextGuild.GetSingleUser(discordUsername);

            if (target == null)
            {
                await ReplyAsync("The provided ID does not match any actual user.");
                return;
            }
            else
            {
                await ReplyAsync($"Erasing all roles and ranks from the user {target.Username} from all guilds. ");
                foreach (var guild in Bot.Instance.guilds.guildList)
                {
                    if (guild.IsGuildMember(target.Id))
                    {
                        await guild.RemoveAllRankRoles(target);
                    }
                }

                await Bot.Instance._data.RemoveFromDatabases(target.Id);
            }
        }


        // --- Admin commands. ---

        [Command("updateuser")]
        public async Task UpdateUser(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            Discord.WebSocket.SocketGuildUser target = contextGuild.GetSingleUser(discordUsername);

            if (target == null)
            {
                await ReplyAsync("The provided ID does not match any actual user.");
                return;
            }

            try
            {
                if (! await Bot.Instance._data.UserTracked(target.Id))
                    {
                    await ReplyAsync($"User {target.Username} not tracked.");
                    return;

                }

                bool ret = await Bot.Instance.UpdateOne(target.Id);

                if (ret)
                {
                    await ReplyAsync($"User {target.Username} updated.");
                    // Print user's rank too.
                    Rank r = await Bot.Instance._data.QueryRank(target.Id);
                    if (r.Digits())
                    {
                        await ReplyAsync($"We see {target.Username}'s rank as {r.FullPrint()}");
                    }
                    else
                    {
                        await ReplyAsync($"We see {target.Username}'s rank as {r.CompactFullPrint()}");
                    }
                }
                else
                {
                    await ReplyAsync("Error during rank update (ret is false).");
                    return;
                }

            }
            catch (RankParsingException)
            {
                await ReplyAsync("Error during rank update (RankParsingException).");
                return;
            }
        }

        [Command("updateall")]
        public async Task UpdateAll()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await ReplyAsync("Running a manual update on all users.");
            _ = Bot.Instance.UpdateAll();
        }

        [Command("backup")]
        public async Task Backup()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await ReplyAsync("Writing the current state of tracking into rsix.json.");
            _ = Bot.Instance.PerformBackup();
        }

        [Command("manualrank")]

        public async Task Manualrank(string discordUsername, string spectralRankName)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            // match user from username

            DiscordGuild guild = Bot.Instance.guilds.byID[Context.Guild.Id];
            var matchedUsers = guild.GetAllUsers(discordUsername);

            if (matchedUsers.Count() == 0)
            {
                await ReplyAsync("There is no user matching the Discord nickname " + discordUsername + ".");
                return;
            }

            if (matchedUsers.Count() > 1)
            {
                await ReplyAsync("Two or more users have the same matching Discord nickname. This command cannot continue.");
                return;
            }

            Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();

            // parse rank string
            Rank r = Ranking.FindRankFromSpectral(spectralRankName);

            if (r.met == Metal.Undefined)
            {
                await ReplyAsync("The inserted rank was not parsed correctly. Remember to put in the spectral name of the rank, such as D or P3.");
                return;
            }

            await ReplyAsync("Setting rank manually for the user " + discordUsername + " to rank " + r.FullPrint());
            await ReplyAsync("Keep in mind that the bot may update your rank later using new data from the R6Tab server.");

            await Bot.Instance.UpdateRoles(rightUser.Id, r);
        }

        [Command("trackuser")]

        public async Task TrackUser(string discordUsername, string nick)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            DiscordGuild guild = Bot.Instance.guilds.byID[Context.Guild.Id];
            var matchedUsers = guild.GetAllUsers(discordUsername);

            if (matchedUsers.Count() == 0)
            {
                await ReplyAsync("There is no user matching the Discord nickname " + discordUsername + ".");
                return;
            }

            if (matchedUsers.Count() > 1)
            {
                await ReplyAsync("Two or more users have the same matching Discord nickname. This command cannot continue.");
                return;
            }

            Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();

            if (rightUser != null)
            {
                // We do not await this one, no need -- it should respond on its own time.
                _ = Bot.Instance.TrackUser(guild, rightUser.Id, nick, Context.Message.Channel.Name);
            }
        }

        [Command("shushplayer")]
        public async Task ShushPlayer(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            var matchedUsers = Context.Guild.Users.Where(x => x.Username.Equals(discordUsername) || x.Nickname.Equals(discordUsername));

            if (matchedUsers.Count() == 0)
            {
                await ReplyAsync("There is no user matching the Discord nickname " + discordUsername + ".");
                return;
            }

            if (matchedUsers.Count() > 1)
            {
                await ReplyAsync("Two or more users have the same matching Discord nickname. This command cannot continue.");
                return;
            }

            Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();
            await Bot.Instance.QuietenUserAndTakeRoles(rightUser.Id);
            await ReplyAsync($"Discord user {rightUser} is now shushed and won't be pinged.");
        }

        [Command("loudenplayer")]
        public async Task LoudenPlayer(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            var matchedUsers = Context.Guild.Users.Where(x => x.Username.Equals(discordUsername) || x.Nickname.Equals(discordUsername));

            if (matchedUsers.Count() == 0)
            {
                await ReplyAsync("There is no user matching the Discord nickname " + discordUsername + ".");
                return;
            }

            if (matchedUsers.Count() > 1)
            {
                await ReplyAsync("Two or more users have the same matching Discord nickname. This command cannot continue.");
                return;
            }

            Discord.WebSocket.SocketGuildUser rightUser = matchedUsers.First();
            // Add the user to any mentionable rank roles.
            await Bot.Instance.LoudenUserAndAddRoles(rightUser.Id);
            await ReplyAsync($"Discord user {rightUser.Username} is now set to loud.");
        }
    }
}
