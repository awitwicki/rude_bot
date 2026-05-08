using RudeBot.Services.UserProfileService;

namespace RudeBot.Tests;

public class ProfileTruncatorTests
{
    [Fact]
    public void Truncate_ShortInput_ReturnsAsIs()
    {
        var input = "Має пса Бобіка.\nГрає в шахи.";
        var result = ProfileTruncator.Truncate(input, 2000);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Truncate_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", ProfileTruncator.Truncate("", 2000));
    }

    [Fact]
    public void Truncate_LongerThanMax_CutsAtLastSentenceBoundary()
    {
        // Three sentences, ~30 chars total. Cap at 20 forces cut after first sentence.
        var input = "Має пса. Любить шахи. І їздить на Tesla.";
        var result = ProfileTruncator.Truncate(input, 20);
        Assert.Equal("Має пса.", result);
    }

    [Fact]
    public void Truncate_LongerThanMax_CutsAtLatestBoundaryInWindow()
    {
        var input = "Перший факт.\nДругий факт. Третій факт довший за межу.";
        var result = ProfileTruncator.Truncate(input, 25);
        // Within first 25 chars: "Перший факт.\nДругий факт.". Backward scan picks the last boundary, the '.' at pos 24.
        Assert.Equal("Перший факт.\nДругий факт.", result);
    }

    [Fact]
    public void Truncate_NullInput_ReturnsEmpty()
    {
        Assert.Equal("", ProfileTruncator.Truncate(null!, 2000));
    }

    [Fact]
    public void Truncate_InputExactlyAtMax_ReturnsAsIs()
    {
        var input = new string('x', 100);
        var result = ProfileTruncator.Truncate(input, 100);
        Assert.Equal(input, result);
        Assert.Equal(100, result.Length);
    }

    [Fact]
    public void Truncate_NoBoundaryInLast200Chars_HardCuts()
    {
        var input = new string('a', 3000);
        var result = ProfileTruncator.Truncate(input, 2000);
        Assert.Equal(2000, result.Length);
        Assert.All(result, c => Assert.Equal('a', c));
    }

    [Fact]
    public void Truncate_QuestionAndExclamationCountAsBoundaries()
    {
        var input = "Є пес? Так! І ще кіт є.";
        var result = ProfileTruncator.Truncate(input, 12);
        Assert.Equal("Є пес? Так!", result);
    }
}
