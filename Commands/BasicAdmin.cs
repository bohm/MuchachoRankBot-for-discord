using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace R6RankBot.Commands
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
            await Bot.Instance.PopulateRoles();
        }

        [Command("resetuser")]
        public async Task ResetUser(ulong id)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            Discord.WebSocket.SocketGuildUser target = Bot.Instance.dwrap.ResidentGuild.Users.FirstOrDefault(x => x.Id == id);
            if (target == null)
            {
                await ReplyAsync("The provided ID does not match any actual user.");
                return;
            }
            else
            {
                await ReplyAsync("Erasing all roles and ranks from the user named " + target.Username);
                await Bot.Instance.dwrap.ClearAllRanks(target);
                await Bot.Instance.RemoveFromDatabases(id);
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

            var matchedUsers = Bot.Instance.dwrap.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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

            try
            {
                string authorR6TabId = await Bot.Instance.QueryMapping(rightUser.Id);

                if (authorR6TabId == null)
                {
                    await ReplyAsync($"User {rightUser.Username} not tracked.");
                    return;
                }

                bool ret = await Bot.Instance.UpdateOne(rightUser.Id);

                if (ret)
                {
                    await ReplyAsync($"User {rightUser.Username} updated.");
                    // Print user's rank too.
                    Rank r = await Bot.Instance.dwrap.GetCurrentRank(authorR6TabId);
                    if (r.Digits())
                    {
                        await ReplyAsync($"We see {rightUser.Username}'s rank as {r.FullPrint()}");
                    }
                    else
                    {
                        await ReplyAsync($"We see {rightUser.Username}'s rank as {r.CompactFullPrint()}");
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
            _ = Bot.Instance.BackupMappings();
        }

        [Command("manualrank")]

        public async Task Manualrank(string discordUsername, string spectralRankName)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            // match user from username

            var matchedUsers = Bot.Instance.dwrap.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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

            var matchedUsers = Bot.Instance.dwrap.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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
                // TODO: Pasted here to be quick. Just refactor to have this as a function.
                try
                {
                    string queryR6ID = await Bot.Instance.QueryMapping(rightUser.Id);
                    if (queryR6ID != null)
                    {
                        await ReplyAsync(rightUser.Username + ": Vas discord ucet uz sledujeme. / We are already tracking your Discord account.");
                        return;
                    }

                    string r6TabId = await TRNHttpProvider.GetID(nick);
                    if (r6TabId == null)
                    {
                        await ReplyAsync(rightUser.Username + ": Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.");
                        return;
                    }
                    await Bot.Instance.InsertIntoMapping(rightUser.Id, r6TabId);
                    await ReplyAsync(rightUser.Username + ": nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme pc v EU. / We now track you as " + nick + "on pc in the EU.");

                    // Update the newly added user.

                    bool ret = await Bot.Instance.UpdateOne(rightUser.Id);

                    if (ret)
                    {
                        // Print user's rank too.
                        Rank r = await Bot.Instance.dwrap.GetCurrentRank(r6TabId);
                        if (r.Digits())
                        {
                            await ReplyAsync(rightUser.Username + ": Aktualne vidime vas rank jako " + r.FullPrint());
                        }
                        else
                        {
                            await ReplyAsync(rightUser.Username + ": Aktualne vidime vas rank jako " + r.CompactFullPrint());
                        }
                    }
                    else
                    {
                        await ReplyAsync(rightUser.Username + ": Stala se chyba pri nastaven noveho ranku.");
                    }


                }
                catch (DoNotTrackException)
                {
                    await ReplyAsync(rightUser.Username + ": Jste Full Chill, vas rank aktualne nebudeme trackovat.");
                    return;
                }
                catch (RankParsingException e)
                {
                    await ReplyAsync(rightUser.Username + ": Nepodarilo se nam najit vas Uplay ucet. Zkontrolujte si, ze jste napsali prezdivku presne i s velkymi pismeny. Je take mozne, ze r6tab.com aktualne nefunguje.");
                    await ReplyAsync("Pro admina: " + e.Message);
                    return;
                }
            }
        }

        [Command("shushplayer")]
        public async Task ShushPlayer(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            var matchedUsers = Bot.Instance.dwrap.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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
            await Bot.Instance.dwrap.RemoveLoudRoles(rightUser);
            await Bot.Instance.ShushPlayer(rightUser.Id);
            await ReplyAsync($"Discord user {rightUser} is now shushed and won't be pinged.");
        }

        [Command("loudenplayer")]
        public async Task LoudenPlayer(string discordUsername)
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            var matchedUsers = Bot.Instance.dwrap.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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
            await Bot.Instance.MakePlayerLoud(rightUser.Id);
            await Bot.Instance.AddLoudRoles(Bot.Instance.dwrap.ResidentGuild, rightUser);
            await ReplyAsync($"Discord user {rightUser.Username} is now set to loud.");
        }
    }
}
