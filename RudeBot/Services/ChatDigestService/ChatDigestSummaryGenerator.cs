using System.Text.RegularExpressions;
using GenerativeAI;
using RudeBot.Domain.Resources;
using RudeBot.Models;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestSummaryGenerator : IChatDigestSummaryGenerator
{
    private const int MaxPromptLength = 500_000;

    private static readonly Regex BoldAsteriskRegex = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex BoldUnderscoreRegex = new(@"__(.+?)__", RegexOptions.Compiled);
    private static readonly Regex StrikethroughRegex = new(@"~~(.+?)~~", RegexOptions.Compiled);
    private static readonly Regex HeadingRegex = new(@"^\s*#+\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex BulletRegex = new(@"^\s*[\*\-]\s+", RegexOptions.Compiled | RegexOptions.Multiline);

    public async Task<string> GenerateSummary(List<ChatMessage> messages)
    {
        var googleAi = new GoogleAi(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_API_KEY")!);
        var googleModel = googleAi.CreateGenerativeModel(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_MODEL_NAME")!);

        var messagesText = string.Join("\n",
            messages.Select(m => $"[{m.CreatedAt:HH:mm}] {m.UserName}: {m.Text}"));

        if (messagesText.Length > MaxPromptLength)
        {
            messagesText = messagesText[..MaxPromptLength];
        }

        var prompt = Resources.SummarizeMessagesPrompt + "\n\n" + messagesText;

        var response = await googleModel.GenerateContentAsync(prompt);
        return StripMarkdown(response.Text());
    }

    private static string StripMarkdown(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

        text = HeadingRegex.Replace(text, "");
        text = BulletRegex.Replace(text, "- ");
        text = BoldAsteriskRegex.Replace(text, "$1");
        text = BoldUnderscoreRegex.Replace(text, "$1");
        text = StrikethroughRegex.Replace(text, "$1");

        return text.Trim();
    }
}
