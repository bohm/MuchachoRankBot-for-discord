using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RankBot.Extensions;

namespace RankBot.Commands
{
    public class ExtBanTracking : CommonBase
    {
        [Command("sus")]
        public async Task TrackSuspiciousUser(string uplayNick)
        {
            string uplayId;
            try
            {
                uplayId = await Bot.Instance.uApi.GetID(uplayNick);
                if (uplayId == null)
                {
                    await ReplyAsync(Context.Message.Author.Username + ": Nepodarilo se nam najit podezrely Uplay ucet. / We failed to find the suspicious Uplay account.");
                    return;
                }

            }
            catch (RankParsingException e)
            {
                await ReplyAsync(Context.Message.Author.Username + ": Nepodarilo se nam najit podezrely Uplay ucet, je mozne, ze tracker nefunguje. / We failed to find the suspicious Uplay account, it is possible that the tracker is down.");
                await ReplyAsync("Pro admina: " + e.Message);
                return;
            }

            try
            {
                await Bot.Instance.bt.InsertSuspect(uplayId, uplayNick, Context.Message.Author.Id, Context.Guild.Id);
                await ReplyAsync($"{Context.Message.Author.Username}: Zacali jsme trackovat podezreleho. Kdyz dostane ban, dame vedet, nebo se zeptejte pomoci prikazu '!banned {uplayNick}'");
            }
            catch (Exception e)
            {
                await ReplyAsync($"{Context.Message.Author.Username}: Nezdarilo se vlozit uzivatele do databaze podezrelych.");
                await ReplyAsync("Pro admina: " + e.Message);
            }
        }

        [Command("banned")]
        public async Task IsBanned(string uplayNickname)
        {
            (bool matched, bool ban) = Bot.Instance.bt.QueryBanByNick(uplayNickname);

            if (!matched)
            {
                await ReplyAsync($"Tracker podezrelych hracu nezna nikoho s nickem/uplay identifikatorem {uplayNickname}. Jestli ucet existuje, muzete ho trackovat prikazem !sus {uplayNickname}");
            }
            else
            {
                if (ban)
                {
                    BanData bd = Bot.Instance.bt.QueryBanData(uplayNickname);
                    string statsDBURL = StatsDB.BuildURL(uplayNickname, bd.UplayId);
                    await ReplyAsync($"Hrac {uplayNickname} ma skutecne od Ubisoftu ban. Ostuda! Vice najdete zde: {statsDBURL} .");
                }
                else
                {
                    await ReplyAsync($"Uzivatel s uplay ID {uplayNickname} je nami trackovany, ale ban zatim nema.");
                }
            }
        }
    }
}
