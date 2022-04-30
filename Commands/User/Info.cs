using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Info : CommonBase
    {
        [Command("prikazy")]
        public async Task InfoCommandAsync()
        {
            await ReplyAsync(@"Uzitecne prikazy:
!track UplayNick -- Bot zacne sledovat vase uspechy a prideli vam vas aktualni rank.
!update -- Bot aktualizuje vas rank na soucasnou hodnotu. Je treba 30 minut pockat mezi dvema aktualizacemi.
!ticho -- Bot vam odstrani role, ktere jdou pingovat v mistnosti #hledame-spoluhrace.
!nahlas -- Bot vam zapne zpet pingovatelne role.
!reset -- Smaze vsechny rankove role a vsechny informace o vas z databaze, zacnete 's cistym stitem'.
!uplay DiscordNick -- Vypise UPlay ucet daneho cloveka (napr. abyste si ho mohli pridat). Pokud ma ve jmene mezery, napiste !uplay ""Pepa Novak"".");

        }
    }
}
