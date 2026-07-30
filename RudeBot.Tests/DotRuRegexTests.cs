using System.Text.RegularExpressions;
using RudeBot.Domain;

namespace RudeBot.Tests;

public class DotRuRegexTests
{
    // PowerBot.Lite matches MessageHandler patterns via Regex.Match(text, pattern, RegexOptions.IgnoreCase) —
    // unanchored, no other options. Mirror that exact call here so this test proves what the bot actually does.
    private static bool Matches(string text) =>
        Regex.Match(text, Consts.DotRuRegex, RegexOptions.IgnoreCase).Success;

    [Theory]
    [InlineData("check this out: https://example.ru/page")]
    [InlineData("www.rbc.ru is a russian site")]
    [InlineData("batman.ru is clearly a domain")]
    [InlineData("site: www.example.ru/index.html")]
    [InlineData("text.ru ")]
    [InlineData("го грати в 2.ru")]
    [InlineData("паляниця.ru")]
    [InlineData("яndex.ru")]
    public void Matches_GenuineDotRuDomains(string text)
    {
        Assert.True(Matches(text));
    }

    [Theory]
    [InlineData("read the manual.rules carefully")]
    [InlineData("not a link: a.rug carpet")]
    [InlineData("smth.rug")]
    [InlineData("sample.rugbyclub")]
    [InlineData("check out xyz.russia news")]
    [InlineData("vodka.ru3 is a fake tld")]
    [InlineData("value is 12.ru5")]
    public void DoesNotMatch_WordsThatMerelyStartWithDotRu(string text)
    {
        Assert.False(Matches(text));
    }

    [Theory]
    [InlineData("перевір інструкцію")]
    [InlineData("мур")]
    [InlineData("гуру")]
    [InlineData("meru")]
    [InlineData("мгу.рф domain")]
    [InlineData("look at .rustic charm")]
    [InlineData("This is truly cool")]
    [InlineData("featuring the rugby team")]
    [InlineData("abc-ru")]
    [InlineData("abcru")]
    public void DoesNotMatch_UnrelatedText(string text)
    {
        Assert.False(Matches(text));
    }
}
