using GenerativeAI;
using RudeBot.Domain.Resources;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestSummaryGenerator : IChatDigestSummaryGenerator
{
    private const int MaxPromptLength = 500_000;

    public async Task<string> GenerateSummary(List<ChatDigestMessage> messages)
    {
        var googleAi = new GoogleAi(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_API_KEY")!);
        var googleModel = googleAi.CreateGenerativeModel(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_MODEL_NAME")!);

        var messagesText = string.Join("\n",
            messages.Select(m => $"[{m.Timestamp:HH:mm}] {m.UserName}: {m.Text}"));

        if (messagesText.Length > MaxPromptLength)
        {
            messagesText = messagesText[..MaxPromptLength];
        }

        var prompt = Resources.SummarizeMessagesPrompt + "\n\n" + messagesText;

        var response = await googleModel.GenerateContentAsync(prompt);
        return response.Text();
    }
}
