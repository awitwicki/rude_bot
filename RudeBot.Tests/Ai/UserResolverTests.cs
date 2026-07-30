using RudeBot.Services.Ai;

namespace RudeBot.Tests.Ai;

public class UserResolverTests
{
    private static RosterEntry Entry(long id, string displayName, params string[] aliases) =>
        new() { UserId = id, DisplayName = displayName, Aliases = aliases };

    private static readonly RosterEntry[] Roster =
    {
        Entry(7, "John Doe", "johndoe"),
        Entry(8, "Petro Petrenko"),
        Entry(9, "Petro Ivanenko", "petya"),
    };

    [Fact]
    public void Resolve_MatchesDisplayNameExactly()
    {
        var result = UserResolver.Resolve(Roster, "John Doe");

        Assert.True(result.IsResolved);
        Assert.Equal(7, result.Entry!.UserId);
    }

    [Fact]
    public void Resolve_IgnoresCaseAndLeadingAt()
    {
        var result = UserResolver.Resolve(Roster, "  @JOHNDOE ");

        Assert.True(result.IsResolved);
        Assert.Equal(7, result.Entry!.UserId);
    }

    [Fact]
    public void Resolve_MatchesAliasWhenDisplayNameDiffers()
    {
        var result = UserResolver.Resolve(Roster, "petya");

        Assert.True(result.IsResolved);
        Assert.Equal(9, result.Entry!.UserId);
    }

    [Fact]
    public void Resolve_MatchesUniquePartial()
    {
        var result = UserResolver.Resolve(Roster, "ivanenko");

        Assert.True(result.IsResolved);
        Assert.Equal(9, result.Entry!.UserId);
    }

    [Fact]
    public void Resolve_ReturnsCandidatesWhenPartialIsAmbiguous()
    {
        var result = UserResolver.Resolve(Roster, "petro");

        Assert.False(result.IsResolved);
        Assert.True(result.IsAmbiguous);
        Assert.Equal(new[] { "Petro Petrenko", "Petro Ivanenko" }, result.Candidates);
    }

    [Fact]
    public void Resolve_PrefersExactMatchOverPartial()
    {
        var roster = new[] { Entry(1, "Ana"), Entry(2, "Anastasia") };

        var result = UserResolver.Resolve(roster, "Ana");

        Assert.True(result.IsResolved);
        Assert.Equal(1, result.Entry!.UserId);
    }

    [Fact]
    public void Resolve_ReturnsCandidatesWhenExactMatchIsAmbiguous()
    {
        var roster = new[] { Entry(1, "Ana"), Entry(2, "Ana Kovalenko", "ana") };

        var result = UserResolver.Resolve(roster, "ana");

        Assert.True(result.IsAmbiguous);
        Assert.Equal(new[] { "Ana", "Ana Kovalenko" }, result.Candidates);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("невідомий")]
    public void Resolve_ReturnsNotFoundForEmptyOrUnknown(string? query)
    {
        var result = UserResolver.Resolve(Roster, query);

        Assert.False(result.IsResolved);
        Assert.False(result.IsAmbiguous);
        Assert.Empty(result.Candidates);
    }
}
