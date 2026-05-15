using NpgsqlTypes;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace RudeBot.Logging;

public static class LoggingBootstrap
{
    private const string Schema = "logs";
    private const string Table = "app_logs";

    public static void Init(string dbConnectionString, LogEventLevel minLevel)
    {
        SelfLog.Enable(System.Console.Error.WriteLine);

        var columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            { "timestamp",        new UtcTimestampColumnWriter() },
            { "level",            new LevelColumnWriter(true, NpgsqlDbType.Text) },
            { "message",          new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
            { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
            { "exception",        new ExceptionColumnWriter(NpgsqlDbType.Text) },
            { "chat_id",          new SinglePropertyColumnWriter("ChatId", PropertyWriteMethod.Raw, NpgsqlDbType.Bigint) },
            { "user_id",          new SinglePropertyColumnWriter("UserId", PropertyWriteMethod.Raw, NpgsqlDbType.Bigint) },
            { "source_context",   new SinglePropertyColumnWriter("SourceContext", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
            { "properties",       new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
        };

        // WriteTo.PostgreSQL() extension (v2.3.0) has no queueLimit parameter.
        // Instantiate PostgreSQLSink directly to access the overload that accepts queueLimit,
        // which caps the in-process queue so it cannot grow unbounded when Postgres is unreachable.
        var pgSink = new PostgreSQLSink(
            connectionString: dbConnectionString,
            tableName: Table,
            period: System.TimeSpan.FromSeconds(5),
            formatProvider: null,
            columnOptions: columnWriters,
            batchSizeLimit: 50,
            queueLimit: 10_000,
            useCopy: true,
            schemaName: Schema,
            needAutoCreateTable: true,
            respectCase: true);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Sink(pgSink)
            .CreateLogger();
    }

    // Npgsql refuses to write a DateTimeOffset with a non-zero offset to a
    // timestamptz column. The stock TimestampColumnWriter hands the event's
    // local-time DateTimeOffset straight through, which crashes every batch on
    // any non-UTC server. Convert to UTC first.
    private sealed class UtcTimestampColumnWriter : ColumnWriterBase
    {
        public UtcTimestampColumnWriter() : base(NpgsqlDbType.TimestampTz) { }

        public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null)
            => logEvent.Timestamp.ToUniversalTime();
    }
}
