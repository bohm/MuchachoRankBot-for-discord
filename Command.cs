using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {


        [Command("prikazy")]
        public async Task Info()
        {
            await ReplyAsync(@"Uzitecne prikazy:
!track UplayNick platforma -- Bot zacne sledovat vase uspechy a prideli vam vas aktualni rank. Mozne platformy jsou {pc, xbox, ps4}.
!ticho -- Bot vam odstrani role, ktere jdou pingovat v mistnosti #hledame-spoluhrace.
!nahlas -- Bot vam zapne zpet pingovatelne role.
!chill -- Bot vas prestane trackovat, smaze vam ranky a nastavi vam roli Full Chill.
!reset -- Smaze vsechny rankove role a vsechny informace o vas z databaze, zacnete 's cistym stitem'.");
        }

        [Command("residence")]
        public async Task Residence()
        {
            if (Bot.IsOperator(Context.Message.Author.Id) && Bot.Instance.ResidentGuild == null)
            {
                Bot.Instance.ResidentGuild = Context.Guild;
                await ReplyAsync("Setting up residence in guild " + Context.Guild.Name + ".");
            }
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
            await Bot.RemoveLoudRoles(Author, Guild);
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
            await Bot.AddLoudRoles(Author, Guild);
            await ReplyAsync(Author.Username + ": Nyni budete notifikovani, kdyz nekdo zapne vasi roli.");
        }

        [Command("chill")]
        public async Task Chill()
        {
            var Author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            await Bot.Instance.StopTracking(Author.Id);
            await Bot.ClearAllRanks(Author);
            var ChillRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == settings.ChillRole);
            if (ChillRole != null)
            {
                await Author.AddRoleAsync(ChillRole);
            }
            await ReplyAsync(Author.Username + ": Uz nebudeme na tomto serveru sledovat vase ranky. Chill on!");
        }

        [Command("reset")]
        public async Task Reset()
        {
            var Author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            await Bot.ClearAllRanks(Author);
            await Bot.Instance.RemoveFromDatabases(Author.Id);
            await ReplyAsync(Author.Username + ": Smazali jsme o vas vsechny informace. Muzete se nechat znovu trackovat.");
        }

        [Command("track")]
        public async Task Track(string nick, string platform)
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

                string r6TabId = await Bot.GetR6TabId(nick, "EU", platform);

                if (r6TabId == null)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.");
                    return;
                }
                await Bot.Instance.InsertIntoMapping(author.Id, r6TabId);
                await ReplyAsync(author.Username + ": nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme pc v EU. / We now track you as " + nick + "on pc in the EU.");

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
                var matchedUsers = Bot.Instance.ResidentGuild.Users.Where(x => x.Username == discordUsername);

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

                        string r6TabId = await Bot.GetR6TabId(nick, "EU", "pc");

                        if (r6TabId == null)
                        {
                            await ReplyAsync(rightUser.Username + ": Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.");
                            return;
                        }
                        await Bot.Instance.InsertIntoMapping(rightUser.Id, r6TabId);
                        await ReplyAsync(rightUser.Username + ": nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme pc v EU. / We now track you as " + nick + "on pc in the EU.");
                    }
                    catch (DoNotTrackException)
                    {
                        await ReplyAsync(rightUser.Username + ": Jste Full Chill, vas rank aktualne nebudeme trackovat.");
                        return;
                    }
                    catch (RankParsingException)
                    {
                        await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
                        return;
                    }
                }
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
                    var RankTuple = await Bot.GetCurrentRank(authorR6TabId);
                    if (RankTuple.Item2 != -1)
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1] + " (presneji, " + settings.TinyLoudRoles[RankTuple.Item2] + ").");
                    }
                    else
                    {
                        await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1]);
                    }
                }
                catch (RankParsingException)
                {
                    await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
                }
            }
        }

        [Command("directrank")]
        public async Task DirectRank(string nick, string platform)
        {
            try
            {
                var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
                string authorR6TabId = await Bot.GetR6TabId(nick, "EU", platform);
                if (authorR6TabId == null)
                {
                    await ReplyAsync(author.Nickname + ": Nebyli jsme schopni nalezt tohoto uzivatele v databazi r6tab.com.");
                    return;
                }

                var RankTuple = await Bot.GetCurrentRank(authorR6TabId);
                if (RankTuple.Item2 != -1)
                {
                    await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1] + " plus " + settings.TinyLoudRoles[RankTuple.Item2]);
                }
                else
                {
                    await ReplyAsync(author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1]);
                }
            }
            catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
            }
        }

        [Command("veryoldrank")]
        public async Task VeryOldRank(string nick, string platform)
        {
            try
            {
                var Author = Context.Message.Author;
                var RankTuple = await Bot.GetCurrentRank(nick, "EU", platform);
                if (RankTuple.Item2 != -1)
                {
                    await ReplyAsync(Author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1] + " plus " + settings.TinyLoudRoles[RankTuple.Item2]);
                }
                else
                {
                    await ReplyAsync(Author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1]);
                }
            }
            catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
            }
        }
    }
}
