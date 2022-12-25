using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.User
{
    public class Track : UserCommonBase
    {
        public static readonly bool SlashCommand = true;

        public Track()
        {
            SlashName = "track";
            SlashDescription = "Bot zacne sledovat vase uspechy a prideli vam vas aktualni rank..";
            ParameterList.Add(new CommandParameter("uplayname", Discord.ApplicationCommandOptionType.String, "Ubisoft jmeno", true));
        }

        public async override Task ProcessCommandAsync(Discord.WebSocket.SocketSlashCommand command)
        {
            var author = (SocketGuildUser)command.User;
            DiscordGuild contextGuild = Bot.Instance.guilds.byID[author.Guild.Id];
            await command.DeferAsync(ephemeral: true);

            var nick = (string)command.Data.Options.First(x => x.Name == "uplayname").Value;

            if (nick == null)
            {
                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = "Chybi parametr 'Ubisoft jmeno', nemuzeme pokracovat.");
                return;
            }

            await LogCommand(contextGuild, author, "/track", $"/track {nick}");

            if (!Bot.Instance.uApi.Online)
            {
                const string errorMsg = "Ubisoft API aktualne neni dostupne, nemuzeme prikaz dokoncit.";
                await command.ModifyOriginalResponseAsync(
                      resp => resp.Content = errorMsg);
                await LogError(contextGuild, author, "/track", errorMsg);
                return;
            }

            try
            {
                string queryR6ID = await Bot.Instance._data.QueryMapping(author.Id);
                if (queryR6ID != null)
                {
                    const string errorMsg = "Vas discord ucet uz sledujeme. / We are already tracking your Discord account.";
                    await command.ModifyOriginalResponseAsync(
                          resp => resp.Content = errorMsg);
                    await LogError(contextGuild, author, "/track", errorMsg);
                    return;
                }

                string r6TabId = await Bot.Instance.uApi.GetID(nick);

                if (r6TabId == null)
                {
                    const string errorMsg = "Nepodarilo se nam najit vas Uplay ucet. / We failed to find your Uplay account data.";
                    await command.ModifyOriginalResponseAsync(
                          resp => resp.Content = errorMsg);
                    await LogError(contextGuild, author, "/track", errorMsg);
                    return;
                }

                await Bot.Instance._data.InsertIntoMapping(author.Id, r6TabId);

                // Update the newly added user.

                bool ret = await Bot.Instance.UpdateOne(author.Id);
                Rank r = SpecialRanks.Undefined;

                if (ret)
                {
                    // Print user's rank too.
                    r = await Bot.Instance.uApi.GetRank(r6TabId);
                }
                else
                {
                    const string errorMsg = "Nalezli jsme ucet, ale update na novy rank se nezdaril.";
                    await command.ModifyOriginalResponseAsync(resp => resp.Content = errorMsg);
                    await LogError(contextGuild, author, "/track", errorMsg);
                    return;
                }

                await command.ModifyOriginalResponseAsync(
                    resp => resp.Content = $"Slinkovali jsme vas Discord s uctem {nick}, aktualne mate rank {r.FullPrint()}");
                return;

            }
            catch (RankParsingException rpe)
            {

                await ReplyAsync($"Nepodarilo se nastavit track, duvod: {rpe.Message}");
                await LogError(contextGuild, author, "/track", rpe.Message);
                return;
            }
        }
    }
}
