using RudeBot.Services.RoastLimiter;

namespace RudeBot.Tests.Services;

public class RoastLimiterTests
{
    private DateTime _now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    private RoastLimiter CreateLimiter() => new(() => _now);

    [Fact]
    public void TryAcquire_SecondTimeSameDay_ReturnsFalse()
    {
        var limiter = CreateLimiter();

        Assert.True(limiter.TryAcquire(1, 7));
        _now = _now.AddHours(13);
        Assert.False(limiter.TryAcquire(1, 7));
    }

    [Fact]
    public void TryAcquire_NextDay_ReturnsTrue()
    {
        var limiter = CreateLimiter();

        Assert.True(limiter.TryAcquire(1, 7));
        _now = _now.AddDays(1);
        Assert.True(limiter.TryAcquire(1, 7));
    }

    [Fact]
    public void TryAcquire_IsPerUserAndPerChat()
    {
        var limiter = CreateLimiter();

        Assert.True(limiter.TryAcquire(1, 7));
        Assert.True(limiter.TryAcquire(1, 8));
        Assert.True(limiter.TryAcquire(2, 7));
    }

    [Fact]
    public void Release_AllowsAnotherAttemptSameDay()
    {
        var limiter = CreateLimiter();

        Assert.True(limiter.TryAcquire(1, 7));
        limiter.Release(1, 7);
        Assert.True(limiter.TryAcquire(1, 7));
    }
}
