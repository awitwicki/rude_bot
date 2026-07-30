using System.Text;
using RudeBot.Domain.Resources;
using RudeBot.Services.ChatContextService;

namespace RudeBot.Services.Ai;

/// <summary>
/// Збирає промпт для Gemini. Тексти профілів сюди навмисно не потрапляють —
/// модель дістає їх сама через ChatRagTool, коли вони справді потрібні.
/// </summary>
public static class AiPromptBuilder
{
    public static string Build(
        long userId,
        string currentUserName,
        string currentMessage,
        IReadOnlyList<ChatContextMessage> context,
        IReadOnlyList<RosterEntry> roster,
        long botUserId,
        long? creatorId)
    {
        var basePrompt = creatorId.HasValue && userId == creatorId.Value
            ? Resources.AiPromptCreator
            : Resources.AiPrompt;

        var sb = new StringBuilder();
        sb.Append(basePrompt).Append("\n\n");

        if (roster.Count > 0)
        {
            sb.Append("Учасники цього чату:\n");

            foreach (var entry in roster)
            {
                sb.Append(entry.DisplayName);

                if (entry.Aliases.Count > 0)
                {
                    sb.Append(" (").Append(string.Join(", ", entry.Aliases)).Append(')');
                }

                sb.Append('\n');
            }

            sb.Append("Якщо для відповіді потрібні подробиці про когось із них — характер, ")
              .Append("минулі повідомлення чи статистика — виклич відповідну функцію, не вигадуй.\n\n");
        }

        if (context.Count > 0)
        {
            sb.Append("Контекст останніх повідомлень в чаті (рядки з позначкою [твоя відповідь] — ")
              .Append("це повідомлення, які ти, надіслав раніше):\n");

            foreach (var msg in context)
            {
                if (msg.UserId == botUserId)
                {
                    sb.Append("[твоя відповідь] ");
                }

                sb.Append(msg.UserName).Append(": ").Append(msg.Text).Append('\n');
            }

            sb.Append('\n');
        }

        sb.Append("Повідомлення від ").Append(currentUserName).Append(":\n").Append(currentMessage);

        return sb.ToString();
    }
}
