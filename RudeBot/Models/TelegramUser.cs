﻿using PowerBot.Lite.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace RudeBot.Models
{
    public class TelegramUser
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public int Karma { get; set; }
        public int RudeCoins { get; set; }
        public int Warns { get; set; }
        public int TotalMessages { get; set; }
        public int TotalBadWords { get; set; }

        public static TelegramUser FromUser(User user)
        {
            return new TelegramUser
            {
                Id = user.Id,
                UserName = user.GetUserMention(),
                Karma = 0,
                TotalMessages = 0,
                RudeCoins = 1000,
                TotalBadWords = 0,
                Warns = 0
            };
        }
    }
}