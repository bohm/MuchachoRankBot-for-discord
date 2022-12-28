using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankBot
{
    partial class Bot
    {
        public async void DatabaseCleanup()
        {
            long absentUsers = 0;
            Console.WriteLine("Beginning cleanup.");
            var databaseCopy = await Data.PrepareBackup();
            foreach (var discordId in databaseCopy.discordRanksDict.Keys)
            {
                bool userPresent = false;
                foreach (var guild in Guilds.byID.Values)
                {
                    var user = guild.GetSingleUser(discordId);
                    if (user != null)
                    {
                        userPresent = true;
                        Console.WriteLine($"User of id {discordId} is present on {guild.GetName()}, has username {user.Username}. ");
                        break;
                    }
                }

                if(!userPresent)
                {
                    Console.WriteLine($"User of id {discordId} is not on any Discord, we should erase them.");
                    absentUsers++;
                }
            }

            Console.WriteLine($"Cleanup complete. Absent users: {absentUsers}");
        }
    }
}
