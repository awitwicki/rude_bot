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
using RudeBot.Services.ChatContextService;
using RudeBot.Services.ChatDigestService;
using RudeBot.Services.DuplicateDetectorService;
using Cron.NET;
using Telegram.Bot;

Console.WriteLine("Starting RudeBot");

// Ensure enough thread pool threads for low-core systems (e.g. Synology NAS)
ThreadPool.SetMinThreads(16, 16);

var botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

// Run bot
var botClient = new CoreBot(botToken);

// Create database if not exists
var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("RUDEBOT_DB_CONNECTION_STRING")!);

var dbContextOptions = optionsBuilder.Options;

await using (var dbContext = new DataContext(dbContextOptions))
{
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Database is synchronized");
}

// Register middlewares and handlers
botClient.RegisterMiddleware<BotMiddleware>()
    .RegisterHandler<BotHandler>()
    .RegisterHandler<ManageHandler>();

// Register services
botClient.RegisterContainers(x =>
{
    x.Register(ctx => dbContextOptions)
        .As<DbContextOptions<DataContext>>()
        .SingleInstance();

    x.RegisterType<DataContext>()
        .AsSelf()
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
       .SingleInstance();

    x.RegisterType<ChatDigestService>()
       .As<IChatDigestService>()
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

    x.RegisterType<CronDaemon>()
        .AsSelf()
        .SingleInstance();

    x.RegisterType<ChatDigestBackgroundService>()
        .As<IStartable>()
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
var chatSettingsService = DIContainerInstance.Container.Resolve<IChatSettingsService>();
await chatSettingsService.LoadAllChatSettings();
Console.WriteLine("Chat settings loaded");

await botClient.StartReceiving();

// Wait for eternity
await Task.Delay(-1);
