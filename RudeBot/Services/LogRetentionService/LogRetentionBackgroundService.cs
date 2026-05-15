using Autofac;
using Cron.NET;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace RudeBot.Services.LogRetentionService;

public class LogRetentionBackgroundService : IStartable
{
    private readonly CronDaemon _cronDaemon;
    private readonly ILogger<LogRetentionBackgroundService> _logger;
    private readonly string _dbConnectionString;

    public LogRetentionBackgroundService(
        CronDaemon cronDaemon,
        ILogger<LogRetentionBackgroundService> logger,
        string dbConnectionString)
    {
        _cronDaemon = cronDaemon;
        _logger = logger;
        _dbConnectionString = dbConnectionString;
    }

    public void Start()
    {
        _cronDaemon.AddJob(LogRetentionConsts.CronExpression, RunCleanup);
        _cronDaemon.Start();
        _logger.LogInformation("LogRetention cron scheduled ({CronExpression}, retention {Days} days)",
            LogRetentionConsts.CronExpression, LogRetentionConsts.RetentionDays);
    }

    public static string BuildCleanupSql(int retentionDays) =>
        $"DELETE FROM logs.app_logs WHERE timestamp < NOW() - INTERVAL '{retentionDays} days';";

    private void RunCleanup()
    {
        try
        {
            using var conn = new NpgsqlConnection(_dbConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(BuildCleanupSql(LogRetentionConsts.RetentionDays), conn);
            var deleted = cmd.ExecuteNonQuery();
            _logger.LogInformation("LogRetention cleanup deleted {RowCount} rows", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogRetention cleanup failed");
        }
    }
}
