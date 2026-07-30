// See https://aka.ms/new-console-template for more information
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PowerBot.Lite;
using RudeBot;
using RudeBot.Common.Services;
using RudeBot.Database;
using RudeBot.Domain;
using RudeBot.Domain.Interfaces;
using RudeBot.Domain.Resources;
using RudeBot.Handlers;
using RudeBot.Managers;
using RudeBot.Services;
using RudeBot.Services.Ai;
using RudeBot.Services.ChatContextService;
using RudeBot.Services.ChatDigestService;
using RudeBot.Services.UserProfileService;
using RudeBot.Services.DuplicateDetectorService;
using RudeBot.Services.LogRetentionService;
using Cron.NET;
using Telegram.Bot;
using RudeBot.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

Console.WriteLine("Starting RudeBot");

// Ensure enough thread pool threads for low-core systems (e.g. Synology NAS)
ThreadPool.SetMinThreads(16, 16);

var botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;
var dbConnectionString = Environment.GetEnvironmentVariable("RUDEBOT_DB_CONNECTION_STRING")!;
var minLevelRaw = Environment.GetEnvironmentVariable("RUDEBOT_LOGGING_MIN_LEVEL");
var minLevel = Enum.TryParse<LogEventLevel>(minLevelRaw, ignoreCase: true, out var lvl) ? lvl : LogEventLevel.Information;

var creatorIdRaw = Environment.GetEnvironmentVariable("RUDEBOT_CREATOR_ID");
if (long.TryParse(creatorIdRaw, out var creatorId))
{
    Consts.CreatorId = creatorId;
    Console.WriteLine($"Creator id loaded: {creatorId}");
}

// Resolve bot's own Telegram user id once so we can recognise our own messages later
var bootstrapBotClient = new TelegramBotClient(botToken);
var botMe = await bootstrapBotClient.GetMe();
Consts.BotUserId = botMe.Id;
Console.WriteLine($"Bot user id loaded: {Consts.BotUserId} (@{botMe.Username})");

// Run bot
var botClient = new CoreBot(botToken);

// Create database if not exists. Migrations must run BEFORE the logger is
// initialised because one of them creates the `logs` schema that the Serilog
// PostgreSQL sink writes into.
var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
optionsBuilder.UseNpgsql(dbConnectionString);

var dbContextOptions = optionsBuilder.Options;

await using (var dbContext = new DataContext(dbContextOptions))
{
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Database is synchronized");

    // Warm up EF model compilation and Npgsql connection so the first real
    // message doesn't pay the 1–3 s cold-start cost.
    _ = await dbContext.UserStats
        .Include(x => x.User)
        .Where(x => false)
        .ToListAsync();
    Console.WriteLine("EF warm-up complete");
}

LoggingBootstrap.Init(dbConnectionString, minLevel);
Log.Information("Logging initialised at minimum level {MinLevel}", minLevel);

// Register middlewares and handlers
botClient.RegisterMiddleware<BotMiddleware>()
    .RegisterHandler<BotHandler>()
    .RegisterHandler<ManageHandler>();

// Register services
botClient.RegisterContainers(x =>
{
    x.RegisterInstance<ILoggerFactory>(new SerilogLoggerFactory(Log.Logger, dispose: false))
        .SingleInstance();
    x.RegisterGeneric(typeof(Logger<>))
        .As(typeof(ILogger<>))
        .SingleInstance();

    x.Register(ctx => dbContextOptions)
        .As<DbContextOptions<DataContext>>()
        .SingleInstance();

    x.RegisterType<DataContext>()
        .AsSelf()
        .InstancePerLifetimeScope();

    x.RegisterType<ChatMessageRepository>()
        .As<IChatMessageRepository>()
        .InstancePerLifetimeScope();

    x.RegisterType<TxtWordsDataset>()
        .WithParameter("data", Resources.BadWordsDataset
            .Split("\n")
            .Select(x => x.Replace("\r", "").ToLower())
        )
        .Keyed<TxtWordsDataset>(Consts.BadWordsService)
        .As<ITxtWordsDataset>()
        .SingleInstance();

    x.RegisterType<TxtWordsDataset>()
        .WithParameter("data", Resources.Advices
            .Split("\n")
        )
        .Keyed<TxtWordsDataset>(Consts.AdvicesService)
        .As<ITxtWordsDataset>()
        .SingleInstance();
    
    x.RegisterType<AllowedChatsService>()
        .WithParameter("input", Environment.GetEnvironmentVariable("RUDEBOT_ALLOWED_CHATS"))
        .As<IAllowedChatsService>()
        .InstancePerLifetimeScope();

    x.RegisterType<UserManager>()
       .As<IUserManager>()
       .InstancePerLifetimeScope();

    x.RegisterType<CatService>()
        .As<ICatService>()
        .InstancePerLifetimeScope();

    x.RegisterType<DuplicateDetectorService>()
       .As<IDuplicateDetectorService>()
       .WithParameter("expireTime", TimeSpan.FromDays(5))
       .SingleInstance();

    x.RegisterType<ChatContextService>()
        .As<IChatContextService>()
        .InstancePerLifetimeScope();

    x.RegisterType<UserProfileService>()
        .As<IUserProfileService>()
        .InstancePerLifetimeScope();

    x.RegisterType<AiResponder>()
        .As<IAiResponder>()
        .InstancePerLifetimeScope();

    x.RegisterType<UserProfileMerger>()
        .As<IUserProfileMerger>()
        .SingleInstance();

    x.RegisterType<ChatSettingsService>()
        .As<IChatSettingsService>()
        .SingleInstance();

    x.Register(_ => new TelegramBotClient(botToken))
        .As<ITelegramBotClient>()
        .SingleInstance();

    x.RegisterType<ChatDigestSummaryGenerator>()
        .As<IChatDigestSummaryGenerator>()
        .SingleInstance();

    x.RegisterType<ChatDigestRunner>()
        .As<IChatDigestRunner>()
        .SingleInstance();

    x.RegisterType<CronDaemon>()
        .AsSelf()
        .SingleInstance();

    x.RegisterType<ChatDigestBackgroundService>()
        .As<IStartable>()
        .SingleInstance();

    x.RegisterType<LogRetentionBackgroundService>()
        .As<IStartable>()
        .WithParameter("dbConnectionString", dbConnectionString)
        .SingleInstance();
    
    x.RegisterType<TeslaChatCounterService>()
        .As<ITeslaChatCounterService>()
        .InstancePerLifetimeScope();
    
    x.RegisterType<DelayService>()
        .As<IDelayService>()
        .InstancePerLifetimeScope();
});

botClient.Build();

// Pre-load chat settings cache before receiving messages
var chatSettingsService = PowerBot.Lite.Services.DIContainerInstance.Container.Resolve<IChatSettingsService>();
await chatSettingsService.LoadAllChatSettings();
Log.Information("Chat settings loaded");

await botClient.StartReceiving();

// Wait for eternity
await Task.Delay(-1);
