using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RankBot.Extensions
{
    /// <summary>
    /// The main highlighting class. We can this of it as an extension of Bot(). Provides a Filter() function independent on the guilds.
    /// </summary>
    class MainHighlighter
    {
        bool fullInit = false;
        private DiscordGuilds _guilds;
        private Dictionary<ulong, RoleHighlighting> individualHLs; // An individual highlighter for each guild, indexed by the guild ID.

        /// <summary>
        /// A constructor that may only be called once the connection with the Discord API is made and we can pass the full list
        /// of all Discord guilds.
        /// </summary>
        /// <param name="allGuilds"></param>
        public MainHighlighter(DiscordGuilds allGuilds)
        {
            individualHLs = new Dictionary<ulong, RoleHighlighting>();
            _guilds = allGuilds;

            foreach((var guildID, var guild) in _guilds.byID)
            {
                RoleHighlighting singleHL = new RoleHighlighting(guild);
                individualHLs.Add(guildID, singleHL);
            }

            fullInit = true;
        }

        /// <summary>
        /// A global filtering function that can be used on all incoming messages.
        /// </summary>
        /// <param name="rawmsg"></param>
        /// <returns></returns>
        public async Task Filter(SocketMessage rawmsg)
        {
            // We refuse to process anything until the full initialization is complete.
            if (! fullInit)
            {
                return;
            }

            SocketUserMessage message = rawmsg as SocketUserMessage;
            if (message is null || message.Author.IsBot)
            {
                return; // Ignore all bot messages and empty messages.
            }

            var contextChannel = message.Channel;
            if (contextChannel is SocketTextChannel guildChannel) // The message may be a DM, so we cast it to determine its type and work only with guild chat.
            {
                ulong sourceGuild = guildChannel.Guild.Id;
                if (!individualHLs.ContainsKey(sourceGuild))
                {
                    return;
                }

                if(!individualHLs[sourceGuild].HighlightChannels().Contains(message.Channel.Name))
                {
                    return; // Ignore all channels except the allowed ones.
                }
                await individualHLs[sourceGuild].Filter(rawmsg);
            }
            else
            {
                return; // Ignore all DMs for filtering (role highlighting) purposes.
            }
        }
    }

    class RoleHighlighting
    {
        private DiscordGuild _dg; // The Discord guild (server) that this instance of RoleHighlighting operates on.
        // One instance should be unique to one server.
        public string RegexMatcher;
        private static List<string> _upperCaseLoudDigitRoles;
        private static List<string> _upperCaseLoudMetalRoles;
        private static List<string> _lowerCaseLoudDigitRoles; // Needs to be initialized from LoudDigitRoles.
        private static List<string> _lowerCaseLoudMetalRoles; // Needs to be initialized from LoudMetalRoles.
        private Dictionary<string, ulong> _roleNameToID;

        public RoleHighlighting(DiscordGuild dg)
        {
            _dg = dg;
            _roleNameToID = new Dictionary<string, ulong>();
            // Initialize internal list of roles.
            _upperCaseLoudDigitRoles = new List<string>(Ranking.LoudDigitRoles);
            _upperCaseLoudMetalRoles = new List<string>(Ranking.LoudMetalRoles);
            _lowerCaseLoudDigitRoles = RoleHighlighting.ListToLowerCase(Ranking.LoudDigitRoles);
            _lowerCaseLoudMetalRoles = RoleHighlighting.ListToLowerCase(Ranking.LoudMetalRoles);

            // Build the regex matching string from the roleset in Settings.
            StringBuilder sb = new StringBuilder();
            // The order is important here, so that Plat 3 will be matched before Plat itself.
            RoleHighlighting.ConcatenateWithOr(sb, _upperCaseLoudDigitRoles, _lowerCaseLoudDigitRoles, _upperCaseLoudMetalRoles, _lowerCaseLoudMetalRoles);
            RoleHighlighting.AppendSpecialOrEndline(sb);
            RegexMatcher = sb.ToString();


            // Populate _roleNameToID.
            foreach (string name in Ranking.LoudMetalRoles)
            {
                SocketRole role = _dg._socket.Roles.FirstOrDefault(x => x.Name == name);
                if (role == null)
                {
                    Console.WriteLine($"The role {name} has not been found in server {_dg.GetName()}. Populate the Discord server with roles before you start this extensions.");
                    throw new Exception();
                }
                else
                {
                    _roleNameToID.Add(name, role.Id);
                }
            }

            foreach (string name in Ranking.LoudDigitRoles)
            {
                SocketRole role = _dg._socket.Roles.FirstOrDefault(x => x.Name == name);
                if (role == null)
                {
                    Console.WriteLine($"The role {name} has not been found in server {_dg.GetName()}. Populate the Discord server with roles before you start this extensions.");
                    throw new Exception();
                }
                else
                {
                    _roleNameToID.Add(name, role.Id);
                }
            }
        }

        public static string FirstToUppercase(string s)
        {
            if(s.Length == 0)
            {
                return s;
            }

            char first = s[0];
            return char.ToUpper(first) + s.Substring(1);
        }

        public static string FirstToLowercase(string s)
        {
            if (s.Length == 0)
            {
                return s;
            }

            char first = s[0];
            return char.ToLower(first) + s.Substring(1);
        }

        public static List<string> ListToLowerCase(string[] upperCaseList)
        {
            List<string> res = new List<string>();
            foreach (string el in upperCaseList)
            {
                res.Add(RoleHighlighting.FirstToLowercase(el));
            }
            return res;
        }


        public static void ConcatenateWithOr(StringBuilder sb, params List<string>[] lists)
        {
            sb.Append('(');
            for (int i = 0; i < lists.Length; i++)
            {
                foreach (string expression in lists[i])
                {
                    if(sb.Length >= 2)
                    {
                        sb.Append('|');
                    }
                    sb.Append(expression);
                }
            }
            sb.Append(')');
        }

        public static void AppendSpecialOrEndline(StringBuilder sb)
        {
            sb.Append(@"(?:\W|$)");
        }

        public List<string> RolesToHighlight(string haystack)
        {
            List<string> ret = new List<string>();
            var matchCollection = Regex.Matches(haystack, RegexMatcher);
            foreach(Match m in matchCollection)
            {
                string canonicalForm = RoleHighlighting.FirstToUppercase(m.Groups[1].Value);
                Console.WriteLine($"Canonical form of match is: {canonicalForm}");
                ret.Add(canonicalForm);
            }

            return ret;
        }

        public List<string> HighlightChannels()
        {
            return _dg.Config.roleHighlightChannels;
        }

        public async Task Filter(SocketMessage rawmsg)
        {
            SocketUserMessage message = rawmsg as SocketUserMessage;
            List<string> roles = RolesToHighlight(rawmsg.Content);

            if (roles.Count == 0)
            {
                return;
            }

            StringBuilder taggedRoles = new StringBuilder();
            bool first = true;
            foreach (string role in roles)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    taggedRoles.Append(" ");
                }
                taggedRoles.Append("<@&");
                taggedRoles.Append(_roleNameToID[role]);
                taggedRoles.Append(">");
            }

            SocketTextChannel responseChannel = (SocketTextChannel)message.Channel;
            await responseChannel.SendMessageAsync(taggedRoles.ToString());
            return;
        }
    }
}
