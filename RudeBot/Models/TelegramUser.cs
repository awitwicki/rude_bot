using PowerBot.Lite.Utils;
using Telegram.Bot.Types;

namespace RudeBot.Models;

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
        long GetSize(long id)
        {
            return (id + 19) % 25 + 7;
        }

        (int, int) Orientation(long id, int maxCount1, int maxCount2)
        {
            return ((int)id % maxCount1, (int)id % 5 % maxCount2);
        }

        var userSize = GetSize(Id);

        float badWordsPercent = 0;
        if (TotalBadWords > 0 && TotalMessages > 0)
        {
            badWordsPercent = TotalBadWords * 100 / TotalMessages;
        }

        var karmaPercent = 0;
        if (Karma > 0 && TotalMessages > 0)
        {
            karmaPercent = Karma * 100 / TotalMessages;
        }

        var orientationTypes = new List<string>() { "Латентний", "Гендерфлюід", "Straight", "" };
        var orientationNames = new List<string>() { "Samsung", "Apple", "Android", "Nokia" };

        var orientationValues = Orientation(Id,
            orientationTypes.Count,
            orientationNames.Count);

        var orientationType = orientationTypes[orientationValues.Item1];
        var orientationName = orientationNames[orientationValues.Item2];

        var orientation = $"{orientationType} {orientationName}";

        var result = $"Юзернейм: {User.UserMention}\n" +
                     $"Карма: `{Karma} ({karmaPercent}%)`\n" +
                     $"🚧 Попереджень: `{Warns}`\n" +
                     $"Повідомлень: `{TotalMessages}`\n" +
                     $"Матюків: `{TotalBadWords} ({badWordsPercent}%)`\n" +
                     $"Rude-коїнів: `{RudeCoins}`💰\n" +
                     $"Довжина: `{userSize}` сантиметрів, ну і гігант...\n" +
                     $"Орієнтація: `{orientation}` користувач";

        return result;
    }

    // TODO: move to other class
    public string BuildWarnMessage()
    {
        var result = $"{User.UserMention}, вам винесено попередження адміна!\n" +
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
        var username = user.FirstName;
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