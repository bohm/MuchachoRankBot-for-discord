using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace R6RankBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {


        [Command("prikazy")]
        public async Task Info()
        {
            await ReplyAsync(@"Uzitecne prikazy:
!track UplayNick -- Bot zacne sledovat vase uspechy a prideli vam vas aktualni rank.
!update -- Bot aktualizuje vas rank na soucasnou hodnotu. Je treba 30 minut pockat mezi dvema aktualizacemi.
!ticho -- Bot vam odstrani role, ktere jdou pingovat v mistnosti #hledame-spoluhrace.
!nahlas -- Bot vam zapne zpet pingovatelne role.
!reset -- Smaze vsechny rankove role a vsechny informace o vas z databaze, zacnete 's cistym stitem'.");
        }


        [Command("populate")]
        public async Task Populate()
        {
            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("R6RankBot has no set server as a residence, it cannot proceed.");
            }
            else if (!Bot.IsOperator(Context.Message.Author.Id))
            {
                await ReplyAsync("This command needs operator privileges.");
            }
            else
            {
                await Bot.Instance.PopulateRoles();
            }
        }

        [Command("ticho")]
        public async Task Quiet()
        {
            var Author = Context.Message.Author;
            var Guild = Context.Guild;
            await Bot.RemoveLoudRoles(Guild, Author);
            await Bot.Instance.ShushPlayer(Author.Id);
            await ReplyAsync(Author.Username + ": Odted nebudete notifikovani, kdyz nekdo oznaci vasi roli. Poslete prikaz !nahlas pro zapnuti notifikaci.");
        }

        [Command("nahlas")]
        public async Task Loud()
        {
            // Add the user to any mentionable rank roles.
            var Author = Context.Message.Author;
            var Guild = Context.Guild;
            await Bot.Instance.MakePlayerLoud(Author.Id);
            await Bot.Instance.AddLoudRoles(Guild, Author);
            await ReplyAsync(Author.Username + ": Nyni budete notifikovani, kdyz nekdo zapne vasi roli.");
        }

        [Command("reset")]
        public async Task Reset()
        {
            var Author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            await Bot.ClearAllRanks(Author);
            await Bot.Instance.RemoveFromDatabases(Author.Id);
            await ReplyAsync(Author.Username + ": Smazali jsme o vas vsechny informace. Muzete se nechat znovu trackovat.");
        }

        [Command("resetuser")]
        public async Task ResetUser(ulong id)
        {
            if (Bot.IsOperator(Context.Message.Author.Id) && Bot.Instance.ResidentGuild != null)
            {
                await Bot.Instance.RemoveFromDatabases(id);
            }
            else
            {
                await ReplyAsync("This command needs operator privileges.");
            }
        }

        [Command("track")]
        public async Task Track(string nick)
        {
            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            try
            {
                string queryR6ID = await Bot.Instance.QueryMapping(author.Id);
                if (queryR6ID != null)
                {
                    await ReplyAsync(author.Username + ": Vas discord ucet uz sledujeme. / We are already tracking your Discord account.");
                    return;
                }

                string r6TabId = await TRNHttpProvider.GetID(nick);

                if (r6TabId == null)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.");
                    return;
                }
                await Bot.Instance.InsertIntoMapping(author.Id, r6TabId);
                await ReplyAsync(author.Username + ": nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme PC. / We now track you as " + nick + " on PC.");

                // Update the newly added user.

                bool ret = await Bot.Instance.UpdateOne(author.Id);

                if (ret)
                {
                    // Print user's rank too.
                    Rank r = await Bot.GetCurrentRank(r6TabId);
                    if (r.Digits())
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.FullPrint());
                    }
                    else
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.CompactFullPrint());
                    }
                }
                else
                {
                    await ReplyAsync(author.Username + ": Stala se chyba pri nastaven noveho ranku.");
                }


            }
            catch (DoNotTrackException)
            {
                await ReplyAsync(author.Username + ": Jste Full Chill, vas rank aktualne nebudeme trackovat.");
                return;
            }
            catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the local Discord admins.");
                return;
            }
        }

        [Command("update")]
        public async Task Update()
        {
            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("R6RankBot has no set server as a residence, it cannot proceed.");
                return;
            }

            try
            {
                var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
                string authorR6TabId = await Bot.Instance.QueryMapping(author.Id);

                if (authorR6TabId == null)
                {
                    await ReplyAsync(author.Username + ": vas rank netrackujeme, tak nemuzeme slouzit.");
                    return;
                }

                bool ret = await Bot.Instance.UpdateOne(author.Id);

                if (ret)
                {
                    await ReplyAsync(author.Username + ": Aktualizovali jsme vase MMR a rank. Nezapomente, ze to jde jen jednou za 30 minut.");
                    // Print user's rank too.
                    Rank r = await Bot.GetCurrentRank(authorR6TabId);
                    if (r.Digits())
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.FullPrint());
                    }
                    else
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.CompactFullPrint());
                    }
                }
                else
                {
                    await ReplyAsync(author.Username + ": Stala se chyba pri aktualizaci ranku, mate stale predchozi rank.");
                } 

            }
            catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the local Discord admins.");
                return;
            }
        }



        [Command("rank")]
        public async Task Rank()
        {
            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            string authorR6TabId = await Bot.Instance.QueryMapping(author.Id);

            if (authorR6TabId == null)
            {
                await ReplyAsync(author.Username + ": vas rank netrackujeme, tak nemuzeme slouzit.");
                return;
            }
            else
            {
                try
                {

                    Rank r = await Bot.GetCurrentRank(authorR6TabId);
                    if (r.Digits())
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.FullPrint());
                    }
                    else
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + r.CompactFullPrint());
                    }
                }
                catch (RankParsingException)
                {
                    await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
                }
            }
        }

        // --- Admin commands. ---

        [Command("residence")]
        public async Task Residence()
        {
            if (Bot.IsOperator(Context.Message.Author.Id) && Bot.Instance.ResidentGuild == null)
            {
                Bot.Instance.ResidentGuild = Context.Guild;
                await ReplyAsync("Setting up residence in guild " + Context.Guild.Name + ".");
                // await Bot.Instance.CleanSlate();
                await Bot.Instance.RoleInit();

            }
        }


        [Command("manualrank")]

        public async Task Manualrank(string discordUsername, string spectralRankName)
        {
            if (!Bot.IsOperator(Context.Message.Author.Id))
            {
                await ReplyAsync("This command needs operator privileges.");
                return;
            }

            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("R6RankBot has no set server as a residence, it cannot proceed.");
                return;
            }

            // match user from username

            var matchedUsers = Bot.Instance.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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
            if (Bot.Instance.ResidentGuild == null)
            {
                await ReplyAsync("R6RankBot has no set server as a residence, it cannot proceed.");
                return;
            }

            if (!Bot.IsOperator(Context.Message.Author.Id))
            {
                await ReplyAsync("This command needs operator privileges.");
                return;
            }
            else
            {
                var matchedUsers = Bot.Instance.ResidentGuild.Users.Where(x => x.Username.Equals(discordUsername));

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
                            Rank r = await Bot.GetCurrentRank(r6TabId);
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
        }


    }
}
