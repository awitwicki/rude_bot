using System.Text;
using RudeBot.Domain.Resources;
using RudeBot.Models;

namespace RudeBot.Services.Ai;

/// <summary>
/// Промпт для /roastme: інструкція + все, що бот знає про користувача (профіль, статистика, його повідомлення).
/// </summary>
public static class RoastPromptBuilder
{
    public const int MaxMessageLength = 300;

    public static string Build(
        string userName,
        UserChatProfile? profile,
        UserChatStats? stats,
        IReadOnlyList<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        sb.Append(Resources.RoastMePrompt).Append("\n\n");

        sb.Append("Мене звати: ").Append(userName).Append('\n');

        if (stats != null)
        {
            sb.Append("Моя статистика в чаті: карма ").Append(stats.Karma)
              .Append(", повідомлень ").Append(stats.TotalMessages)
              .Append(", матюків ").Append(stats.TotalBadWords)
              .Append(", попереджень ").Append(stats.Warns)
              .Append('\n');
        }

        if (profile != null && !string.IsNullOrWhiteSpace(profile.Profile))
        {
            sb.Append("\nЩо про мене відомо:\n").Append(profile.Profile).Append('\n');
        }

        if (messages.Count > 0)
        {
            sb.Append("\nМої останні повідомлення в чаті:\n");

            foreach (var msg in messages)
            {
                var text = msg.Text.Length > MaxMessageLength
                    ? msg.Text[..MaxMessageLength] + "…"
                    : msg.Text;

                sb.Append("- ").Append(text).Append('\n');
            }
        }

        return sb.ToString();
    }
}
