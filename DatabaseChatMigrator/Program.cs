// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using RudeBot.Services;
using RudeBot;
using RudeBot.Models;
using DatabaseChatMigrator;
using System.Text.RegularExpressions;
using RudeBot.Database;
using Microsoft.EntityFrameworkCore;


// Read bad words
var badWordsReader = new TxtWordsDatasetReader("../../../../RudeBot/Resources/Badwords.txt");

// Read chat and parse
List<UserChatStats> users = new List<UserChatStats>();
JsonChat chat;

// Export chat history to .json file and import in this project as result.json
using (StreamReader r = new StreamReader("result.json"))
{
    string json = r.ReadToEnd();
    chat = JsonConvert.DeserializeObject<JsonChat>(json);
}

// Parse all users to database
chat.messages
    .Where(x => x.action != "invite_members")
    .Where(x => x.actor == null)
    .Where(x => !x.from_id.Contains("channel"))
    .Where(x => !x.type.Contains("action"))
    .GroupBy(x => x.from_id)
    .ToList()
    .ForEach(x => {
        // Get only simple messages
        List<string> messagesWithText = x.Where(y => y.text is string && y.text != "")
            //.Where(y => y.text != "")
            .Select(y => y.text)
            .Cast<string>()
            .ToList();

        UserChatStats newUsr = new UserChatStats
        {
            ChatId = long.Parse($"-100{chat.id}"), 
            RudeCoins = 1000,
            UserId = long.Parse(x.Key.Replace("user", "")),
            User = new TelegramUser
            {
                Id = long.Parse(x.Key.Replace("user", "")),
                UserName = x.First().from,
                UserMention = x.First().from,
            },
            TotalMessages = x.Count(),
            TotalBadWords = messagesWithText.Count(y => badWordsReader.GetWords().Any(a => a.Contains(y)))
        };

        users.Add(newUsr);
    });

// Filter users without name 
users = users
    .Where(x => x.User.UserName != null)
    .ToList();

Console.WriteLine($"Total parsed {users.Count} users");


// Calculate karma

// Positive karma messages
var dictkarmaMessages = chat.messages
    .Where(x => x.action != "invite_members")
    .Where(x => x.actor == null)
    .Where(x => !x.from_id.Contains("channel"))
    .Where(x => !x.type.Contains("action"))
    .Where(x => x.reply_to_message_id.HasValue)
    .Where(y => y.text is string && y.text != "")
    .Where(x => Regex.Match((string)x.text, Consts.TnxWordsRegex, RegexOptions.IgnoreCase).Success)
    .ToList();

Console.WriteLine($"Process positive karma from {dictkarmaMessages.Count} messages...");

dictkarmaMessages.ForEach(x =>
{
    int userMessageId = x.reply_to_message_id.Value;
    var message = chat.messages.FirstOrDefault(y => y.id == userMessageId);

    if (message != null && message.from_id != null && !message.from_id.Contains("channel"))
    {
        // Get user id
        long userId = long.Parse(message.from_id.Replace("user", ""));

        // Find in telegramUsersList
        var tempUsr = users.FirstOrDefault(y => y.User.Id == userId);
        if (tempUsr != null)
            tempUsr.Karma++;
    }
});

Console.WriteLine($"Total parsed {users.Where(x => x.Karma > 0).Count()} users with positive karma");

// Negative karma messages
dictkarmaMessages = chat.messages
    .Where(x => x.action != "invite_members")
    .Where(x => x.actor == null)
    .Where(x => !x.from_id.Contains("channel"))
    .Where(x => !x.type.Contains("action"))
    .Where(x => x.reply_to_message_id.HasValue)
    .Where(y => y.text is string && y.text != "")
    .Where(x => Regex.Match((string)x.text, "^-$", RegexOptions.IgnoreCase).Success)
    .ToList();

Console.WriteLine($"Process negative karma from {dictkarmaMessages.Count} messages...");

dictkarmaMessages.ForEach(x =>
{
    int userMessageId = x.reply_to_message_id.Value;
    var message = chat.messages.FirstOrDefault(y => y.id == userMessageId);

    if (message != null && message.from_id != null && !message.from_id.Contains("channel"))
    {
        // Get user id
        long userId = long.Parse(message.from_id.Replace("user", ""));

        // Find in telegramUsersList
        var tempUsr = users.FirstOrDefault(y => y.User.Id == userId);
        if (tempUsr != null)
            tempUsr.Karma--;
    }
});

Console.WriteLine($"Total parsed {users.Where(x => x.Karma < 0).Count()} users with negative karma");

// Save to database
// SET YOUR OWN DATABASE PATH OF DataContext in OnConfiguring() method
var _dbContext = new DataContext();

_dbContext.Database.Migrate();

// Add users to database
_dbContext.AddRange(users.Select(x => x.User));
_dbContext.SaveChanges();

// Add ChatInfos to database
_dbContext.AddRange(users);
_dbContext.SaveChanges();

Console.WriteLine("Database successfully saved");
