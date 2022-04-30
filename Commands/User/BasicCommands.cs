using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RankBot
{
    public class BasicCommands : CommonBase
    {


        [Command("prikazy")]
        public async Task Info()
        {
            await ReplyAsync(@"Uzitecne prikazy:
!track UplayNick -- Bot zacne sledovat vase uspechy a prideli vam vas aktualni rank.
!update -- Bot aktualizuje vas rank na soucasnou hodnotu. Je treba 30 minut pockat mezi dvema aktualizacemi.
!ticho -- Bot vam odstrani role, ktere jdou pingovat v mistnosti #hledame-spoluhrace.
!nahlas -- Bot vam zapne zpet pingovatelne role.
!reset -- Smaze vsechny rankove role a vsechny informace o vas z databaze, zacnete 's cistym stitem'.
!uplay DiscordNick -- Vypise UPlay ucet daneho cloveka (napr. abyste si ho mohli pridat). Pokud ma ve jmene mezery, napiste !uplay ""Pepa Novak"".");

        }

        [Command("ticho")]
        public async Task Quiet()
        {
            if (! await InstanceCheck())
            {
                return;
            }

            var author = Context.Message.Author;

            // Log command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/ticho");

            await Bot.Instance.QuietenUserAndTakeRoles(author.Id);
            await ReplyAsync(author.Username + ": Odted nebudete notifikovani, kdyz nekdo oznaci vasi roli. Poslete prikaz !nahlas pro zapnuti notifikaci.");
        }

        [Command("nahlas")]
        public async Task Loud()
        {
            if (! await InstanceCheck())
            {
                return;
            }
            // Add the user to any mentionable rank roles.
            var author = Context.Message.Author;

            // Log command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/nahlas");

            await Bot.Instance.LoudenUserAndAddRoles(author.Id);
            await ReplyAsync(author.Username + ": Nyni budete notifikovani, kdyz nekdo zapne vasi roli.");
        }

        [Command("reset")]
        public async Task Reset()
        {
            if (!await InstanceCheck())
            {
                return;
            }

            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/reset");

            foreach (DiscordGuild g in Bot.Instance.guilds.byID.Values)
            {
                if (g.IsGuildMember(author.Id))
                {
                    await g.RemoveAllRankRoles(author.Id);
                    await Bot.Instance._data.RemoveFromDatabases(author.Id);
                    await ReplyAsync(author.Username + ": Smazali jsme o vas vsechny informace. Muzete se nechat znovu trackovat.");
                }
            }
        }
        [Command("track")]
        public async Task Track(string nick)
        {
            if (!await InstanceCheck())
            {
                return;
            }

            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

            // Log the command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/track", $"/track {nick}");

            try
            {
                string queryR6ID = await Bot.Instance._data.QueryMapping(author.Id);
                if (queryR6ID != null)
                {
                    await ReplyAsync(author.Username + ": Vas discord ucet uz sledujeme. / We are already tracking your Discord account.");
                    return;
                }

                string r6TabId = await Bot.Instance.uApi.GetID(nick);

                if (r6TabId == null)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.");
                    return;
                }
                await Bot.Instance._data.InsertIntoMapping(author.Id, r6TabId);
                await ReplyAsync(author.Username + ": nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme PC. / We now track you as " + nick + " on PC.");

                // Update the newly added user.

                bool ret = await Bot.Instance.UpdateOne(author.Id);

                if (ret)
                {
                    // Print user's rank too.
                    Rank r = await Bot.Instance.uApi.GetRank(r6TabId);
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
            catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the local Discord admins.");
                return;
            }
        }

        [Command("update")]
        public async Task Update()
        {
            if (!await InstanceCheck())
            {
                return;
            }

            try
            {
                var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;

                // Log the command.
                var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
                await LogCommand(sourceGuild, author, "/update");

                string authorR6TabId = await Bot.Instance._data.QueryMapping(author.Id);

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
                    Rank r = await Bot.Instance.uApi.GetRank(authorR6TabId);
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
            if (!await InstanceCheck())
            {
                return;
            }

            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            string authorR6TabId = await Bot.Instance._data.QueryMapping(author.Id);

            // Log the command.
            var sourceGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            await LogCommand(sourceGuild, author, "/rank");

            if (authorR6TabId == null)
            {
                await ReplyAsync(author.Username + ": vas rank netrackujeme, tak nemuzeme slouzit.");
                return;
            }
            else
            {
                try
                {

                    Rank r = await Bot.Instance.uApi.GetRank(authorR6TabId);
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


        [Command("uplay")]
        public async Task Uplay(string discordNick)
        {
            if (!await InstanceCheck())
            {
                return;
            }
            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];

            // Log the command.
            await LogCommand(contextGuild, author, "/uplay", $"/uplay {discordNick}");

            var target = contextGuild.GetSingleUser(discordNick);
            if (target == null)
            {
                await ReplyAsync(author.Username + ": Nenasli jsme cloveka ani podle prezdivky, ani podle Discord jmena.");
                return;
            }

            if (Bot.Instance._data.DiscordUplay.ContainsKey(target.Id))
            {
                string uplayId = Bot.Instance._data.DiscordUplay[target.Id];
                // This is only the uplay id, the user name might be different. We have to query a tracker to get the current
                // Uplay user name.
                string uplayName = "";
                try
                {
                    uplayName = await Bot.Instance.uApi.GetUplayName(uplayId);
                    if (uplayName == null || uplayName.Length == 0)
                    {
                        throw new RankParsingException("Returned uplayName is empty.");
                    }
                } catch(Exception r)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se ziskat prezdivku z trackeru.");
                    await ReplyAsync("Admin info: " + r.Message);
                    return;
                }
                await ReplyAsync($"{author.Username}: Clen {discordNick} se na Uplayi jmenuje \"{uplayName}\".");
                return;
            }
            else
            {
                await ReplyAsync(author.Username + ": Discord uzivatel " + discordNick + " existuje, ale nenasli jsme ho v databazi ranku.");
                return;
            }
        }


        [Command("mmr")]
        public async Task QueryMMRAsync(string discordNick)
        {
            if (!await InstanceCheck())
            {
                return;
            }
            var author = (Discord.WebSocket.SocketGuildUser)Context.Message.Author;
            DiscordGuild contextGuild = Bot.Instance.guilds.byID[Context.Guild.Id];
            var target = contextGuild.GetSingleUser(discordNick);

            // Log the command.
            await LogCommand(contextGuild, author, "/mmr", $"/mmr {discordNick}");

            if (target == null)
            {
                await ReplyAsync(author.Username + ": Nenasli jsme cloveka ani podle prezdivky, ani podle Discord jmena.");
                return;
            }

            if (Bot.Instance._data.DiscordUplay.ContainsKey(target.Id))
            {
                string uplayId = Bot.Instance._data.DiscordUplay[target.Id];
                int mmr = -1;
                try
                {
                    mmr = await Bot.Instance.uApi.GetMMR(uplayId);
                    if (mmr < 0)
                    {
                        throw new RankParsingException("Returned MMR is less than 0, that is almost surely wrong.");
                    }
                }
                catch (Exception r)
                {
                    await ReplyAsync(author.Username + ": Nepodarilo se ziskat MMR z trackeru.");
                    await ReplyAsync("Admin info: " + r.Message);
                    return;
                }
                await ReplyAsync($"{author.Username}: Clen {discordNick} ma {mmr} MMR.");
                return;
            }
            else
            {
                await ReplyAsync(author.Username + ": Discord uzivatel " + discordNick + " existuje, ale nenasli jsme ho v databazi ranku.");
                return;
            }
        }



    }
}
