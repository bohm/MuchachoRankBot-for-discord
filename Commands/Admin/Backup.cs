﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RankBot.Commands.Admin
{
    public class Backup : AdminCommonBase
    {
        public static readonly string Name = "!backup";
        public static readonly string Description = "Backs up the current memory of the bot into the Discord message and into the secondary json backup.";
        [Command("backup")]
        public async Task BackupCommand()
        {
            if (!(await InstanceCheck() && await OperatorCheck(Context.Message.Author.Id)))
            {
                return;
            }

            await ReplyAsync("Writing the current state of tracking into rsix.json.");
            _ = Bot.Instance.PerformBackup();
        }
    }
}
