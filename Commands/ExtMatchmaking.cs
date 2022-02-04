using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RankBot.Extensions;

namespace RankBot.Commands
{
    public class ExtMatchmaking : CommonBase
    {
        [Command("matchmake")]
        public async Task Matchmake(params string[] users)
        {
            if (users.Length != 10 && users.Length != 20)
            {
                await ReplyAsync($"The command needs exactly 10 or 20 user nicknames, you have provided {users.Length}. Use double quotes for nicknames with special characters, such as \"Pepa Novak\".");
            }
            else
            {
                await ReplyAsync("Trying to matchmake, please give me a few seconds.");
                Matchmaking m = new Matchmaking();
                _ = m.BuildTeams(Bot.Instance, Context.Guild.Id, Context.Channel, users);
            }

        }
    }
}
    