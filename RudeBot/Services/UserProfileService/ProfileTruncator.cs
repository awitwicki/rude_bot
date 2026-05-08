namespace RudeBot.Services.UserProfileService;

public static class ProfileTruncator
{
    private const int MinFallbackWindow = 200;

    public static string Truncate(string input, int maxChars)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxChars)
            return input ?? string.Empty;

        var window = input.AsSpan(0, maxChars);
        var lastBoundary = -1;
        for (var i = window.Length - 1; i >= 0; i--)
        {
            var c = window[i];
            if (c == '.' || c == '!' || c == '?' || c == '\n')
            {
                lastBoundary = i;
                break;
            }
        }

        if (lastBoundary >= 0 && lastBoundary >= maxChars - MinFallbackWindow)
            return input.Substring(0, lastBoundary + 1);

        return input.Substring(0, maxChars);
    }
}
