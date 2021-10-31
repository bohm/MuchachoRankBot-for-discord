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

        /// <summary>
        /// Initialization that is only possible when Discord API is fully operational. It may not be possible at construction time.
        /// </summary>
        public void DelayedInit()
        {
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

        public async Task Filter(SocketMessage rawmsg)
        {
            var message = rawmsg as SocketUserMessage;
            if (message is null || message.Author.IsBot)
            {
                return; // Ignore all bot messages and empty messages.
            }
            if (!Settings.RoleHighlightChannels.Contains(message.Channel.Name))
            {
                return; // Ignore all channels except the allowed ones.
            }

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
