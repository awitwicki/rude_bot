using PowerBot.Lite.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace RudeBot.Models
{
    public class UserChatStats
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public long UserId { get; set; }
        public TelegramUser User { get; set; }
        public int Karma { get; set; }
        public int RudeCoins { get; set; }
        public int Warns { get; set; }
        public int TotalMessages { get; set; }
        public int TotalBadWords { get; set; }

        public static UserChatStats FromChat(Chat chat)
        {
            return new UserChatStats
            {
                ChatId = chat.Id,
                Karma = 0,
                TotalMessages = 0,
                RudeCoins = 1000,
                TotalBadWords = 0,
                Warns = 0
            };
        }

        // TODO: move to other class
        public string BuildInfoString()
        {
            long getSize(long id)
            {
                return (id + 6) % 15 + 7;
            }

            (int, int) orientation(long id)
            {
                return ((int)id % 3, (int)id % 5 % 2);
            }

            long userSize = getSize(Id);

            float BadWordsPercent = 0;
            if (TotalBadWords > 0 && TotalMessages > 0)
            {
                BadWordsPercent = TotalBadWords * 100 / TotalMessages;
            }

            float karmaPercent = 0;
            if (Karma > 0 && TotalMessages > 0)
            {
                karmaPercent = Karma * 100 / TotalMessages;
            }

            List<string> orientationTypes = new List<string>() { "Латентний", "Гендерфлюід", "" };
            List<string> orientationNames = new List<string>() { "Android", "Apple" };

            (int, int) orientationValues = orientation(Id);

            string orientationType = orientationTypes[orientationValues.Item1];
            string orientationName = orientationNames[orientationValues.Item2];

            string result = $"Юзернейм: {User.UserMention}\n" +
               $"Карма: `{Karma} ({karmaPercent}%)`\n" +
               $"🚧 Попереджень: `{Warns}`\n" +
               $"Повідомлень: `{TotalMessages}`\n" +
               $"Матюків: `{TotalBadWords} ({BadWordsPercent}%)`\n" +
               $"Rude-коїнів: `{RudeCoins}`💰\n" +
               $"Довжина: `{userSize}` сантиметрів, ну і гігант...\n" +
               $"Орієнтація: `{orientationType} {orientationName}` користувач";

            return result;
        }

        // TODO: move to other class
        public string BuildWarnMessage()
        {
            string result = $"{User.UserMention}, вам винесено попередження адміна!\n" +
                $"Треба думати що ви пишете, \n" +
                $"ви маєте вже {Warns} попередження!\n\n" +
                $"1 попередження - будь-який адмін може заборонити медіа/стікери/ввести ліміт повідомлень!\n" +
                $"2 попередження - мют на день (або тиждень, на розсуд адміна)!\n" +
                $"3 попередження - бан!\n\n" +
                $"Адміни вирішать твою долю:";

            return result;
        }
    }
    public class TelegramUser
    {
        public long Id { get; set; }
        public string UserMention { get; set; }
        public string UserName { get; set; }

        public static TelegramUser FromUser(User user)
        {
            string username = user.FirstName;
            if (user.LastName != null)
                username += " " + user.LastName;

            return new TelegramUser
            {
                Id = user.Id,
                UserMention = user.GetUserMention(),
                UserName = username
            };
        }
    }
}
