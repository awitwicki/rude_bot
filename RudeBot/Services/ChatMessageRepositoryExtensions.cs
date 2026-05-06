using RudeBot.Models;
using Telegram.Bot.Types;

namespace RudeBot.Services;

public static class ChatMessageRepositoryExtensions
{
    public static async Task PersistBotSentAsync(this IChatMessageRepository repo, Message sentMessage, string text)
    {
        try
        {
            await repo.AddAsync(new ChatMessage
            {
                ChatId = sentMessage.Chat.Id,
                UserId = sentMessage.From?.Id ?? 0,
                UserName = sentMessage.From?.Username ?? "rude кіт",
                Text = text,
                MessageId = sentMessage.MessageId,
                CreatedAt = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Bot ChatMessage persist: {ex}");
        }
    }
}
