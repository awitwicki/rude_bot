namespace RudeBot.Common.Helpers;

public static class MarkdownHelper
{
    public static string EscapeTelegramMarkdown(string text)
    {
        // Telegram legacy Markdown special chars: _ * ` [
        return text
            .Replace("\\", "\\\\")
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("`", "\\`")
            .Replace("[", "\\[");
    }
}
