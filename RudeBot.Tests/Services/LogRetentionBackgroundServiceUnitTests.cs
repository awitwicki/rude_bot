using Cron.NET;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RudeBot.Services.LogRetentionService;

namespace RudeBot.Tests.Services;

public class LogRetentionBackgroundServiceUnitTests
{
    private readonly CronDaemon _cronDaemon;
    private readonly ILogger<LogRetentionBackgroundService> _logger;

    public LogRetentionBackgroundServiceUnitTests()
    {
        _cronDaemon = Substitute.For<CronDaemon>();
        _logger = Substitute.For<ILogger<LogRetentionBackgroundService>>();
    }

    [Fact]
    public void Start_RegistersDailyCronJob()
    {
        var service = new LogRetentionBackgroundService(_cronDaemon, _logger, dbConnectionString: "Host=x");
        service.Start();

        _cronDaemon.Received(1).AddJob(LogRetentionConsts.CronExpression, Arg.Any<Action>());
        _cronDaemon.Received(1).Start();
    }

    [Fact]
    public void BuildCleanupSql_UsesConfiguredRetentionDays()
    {
        var sql = LogRetentionBackgroundService.BuildCleanupSql(LogRetentionConsts.RetentionDays);

        Assert.Contains("DELETE FROM logs.app_logs", sql);
        Assert.Contains($"INTERVAL '{LogRetentionConsts.RetentionDays} days'", sql);
    }
}
