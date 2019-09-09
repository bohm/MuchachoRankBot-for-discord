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
            } else if (!Bot.IsOperator(Context.Message.Author.Id))
            {
                await ReplyAsync("This command needs operator privileges.");
            } else
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
            var Author = (Discord.WebSocket.SocketGuildUser) Context.Message.Author;

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
            var Author = (Discord.WebSocket.SocketGuildUser) Context.Message.Author;
            try
            {
                Tuple<string, string> query = await Bot.Instance.QueryMapping(Author.Id);
                if (query != null)
                {
                    await ReplyAsync(Author.Username + ": Vase uspechy uz sledujeme pod prezdivkou " + query.Item1 + ", nebudeme pridavat dalsi.");
                    return;
                }
                // TODO: check validity of nick + platform, plus test if there is actually any rank under this nickname.
                await Bot.Instance.InsertIntoMapping(Author.Id, nick, platform);
                await ReplyAsync(Author.Username + ": Nove sledujeme vase uspechy pod prezdivkou " + nick + " na platforme " + platform + ".");
            }
            catch (DoNotTrackException)
            {
                await ReplyAsync(Author.Username + ": Jste Full Chill, vas rank aktualne nebudeme trackovat.");
                return;
            }
 

        }
        
        [Command("rank")]
        public async Task Rank()
        {
            var author = (Discord.WebSocket.SocketGuildUser) Context.Message.Author;
            Tuple<string, string> query = await Bot.Instance.QueryMapping(author.Id);

            if (query == null)
            {
                await ReplyAsync(author.Username + ": Jste Full Chill, vas rank netrackujeme, tak nemuzeme slouzit.");
                return;
            }
            else
            {
                try
                {
                    var RankTuple = await Bot.GetCurrentRank(query.Item1, "EU", query.Item2);
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

        [Command("oldrank")]
        public async Task Oldrank(string Nick, string Platform)
        {
            try
            {
                var Author = Context.Message.Author;
                var RankTuple = await Bot.GetCurrentRank(Nick, "EU", Platform);
                if (RankTuple.Item2 != -1)
                {
                    await ReplyAsync(Author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1] + " plus " + settings.TinyLoudRoles[RankTuple.Item2]);
                } else
                {
                    await ReplyAsync(Author.Username + ": Aktualne vidime vas rank jako " + settings.BigLoudRoles[RankTuple.Item1]);
                }
            } catch (RankParsingException)
            {
                await ReplyAsync("Communication to the R6Tab server failed. Please try again or contact the Discord admins.");
            }
        }

        [Command("veryoldrank")]
        public async Task Veryoldrank(string nick, string platform)
        {
            platform = platform.ToLower();
            switch (platform)
            {
                case "pc":
                    platform = "uplay";
                    break;
                case "xbox":
                    platform = "xbl";
                    break;
                case "ps4":
                    platform = "psn";
                    break;
                default:
                    platform = null;
                    break;
            }
            if (platform != null)
            {
                string url = "https://r6tab.com/api/search.php?platform=" + platform + "&search=" + nick;
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                string source = null;
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    source = await response.Content.ReadAsStringAsync();
                }
                string subStringResult = "totalresults";
                string test = source.Substring(source.IndexOf(subStringResult) + 14, 1);
                if (Int32.Parse(test) != 0)
                {
                    string subStringId = "p_id";
                    source = source.Substring(source.IndexOf(subStringId) + 7, 36);
                    url = "https://r6tab.com/api/player.php?p_id=" + source;
                    client = new HttpClient();
                    response = await client.GetAsync(url);
                    source = null;
                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        source = await response.Content.ReadAsStringAsync();
                    }
                    string subStringCurRank = "p_currentrank";
                    string subStringMaxRank = "p_maxrank";
                    string p_currentrank = null;
                    string p_maxrank = null;
                    p_currentrank = source.Substring(source.IndexOf(subStringCurRank) + 15, 2);
                    p_maxrank = source.Substring(source.IndexOf(subStringMaxRank) + 11, 2);
                    string[] ranks = {"unrank",
                            "Copper 4","Copper 3","Copper 2","Copper 1",
                            "Bronze 4", "Bronze 3", "Bronze 2", "Bronze 1",
                            "Silver 4", "Silver 3", "Silver 2", "Silver 1",
                            "Gold 4", "Gold 3", "Gold 2", "Gold 1",
                            "Platinum 3", "Platinum 2", "Platinum 1", "Diamond"};
                    if(Regex.IsMatch(p_currentrank.Substring(1), ","))
                    {
                        p_currentrank = p_currentrank.Substring(0, 1);
                    }
                    if (Regex.IsMatch(p_maxrank.Substring(1), ","))
                    {
                        p_maxrank = p_maxrank.Substring(0, 1);
                    }
                    if (source != null)
                    {
                        string resultF = ranks[Int32.Parse(p_currentrank)];
                        string result = ranks[Int32.Parse(p_maxrank)];
                        var Builder = new EmbedBuilder();
                        Builder.WithDescription("Your current rank is " + resultF + " \n" +
                            "Your max rank is " + result + "\n New(or not new) role " + result);
                        if (Regex.IsMatch(result.Substring(0), "C"))
                            Builder.WithColor(0x995500);
                        else if (Regex.IsMatch(result.Substring(0), "B"))
                            Builder.WithColor(0xff9600);
                        else if (Regex.IsMatch(result.Substring(0), "S"))
                            Builder.WithColor(0x8c8c8c);
                        else if (Regex.IsMatch(result.Substring(0), "G"))
                            Builder.WithColor(0xffff00);
                        else if (Regex.IsMatch(result.Substring(0), "P"))
                            Builder.WithColor(0x00ffff);
                        else if (Regex.IsMatch(result.Substring(0), "D"))
                            Builder.WithColor(0x9900ff);
                        await Context.Channel.SendMessageAsync("", false, Builder.Build());
                        var user = Context.User;
                        var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToString() == result);
                        for (int i = 0; i < ranks.Length; i++)
                        {
                            role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToString() == ranks[i]);
                            await (user as IGuildUser).RemoveRoleAsync(role);
                        }
                        role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToString() == result);
                        await (user as IGuildUser).AddRoleAsync(role);
                        Logger.LogMessageToFile(user.Username+" get role/rank "+ result);
                    }
                    else
                    {
                        await ReplyAsync("ERR:nullSourceInfo");
                    }
                } else
                {
                    await ReplyAsync("Please, type the correct user nickname");
                }
            }
            else
            {
                await ReplyAsync("Please, type the correct platform name");
            }
        }
    }
}
