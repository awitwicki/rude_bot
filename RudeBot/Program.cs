// See https://aka.ms/new-console-template for more information
using Autofac;
using Microsoft.EntityFrameworkCore;
using PowerBot.Lite;
using RudeBot;
using RudeBot.Database;
using RudeBot.Domain;
using RudeBot.Domain.Resources;
using RudeBot.Handlers;
using RudeBot.Managers;
using RudeBot.Services;
using RudeBot.Services.DuplicateDetectorService;

Console.WriteLine("Starting RudeBot");

var botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

// Run bot
var botClient = new CoreBot(botToken);

// Create database if not exists
await using (var dbContext = new DataContext())
{
    dbContext.Database.Migrate();
    Console.WriteLine("Database is synchronized");
}

// Register middlewares and handlers
botClient.RegisterMiddleware<BotMiddleware>()
    .RegisterHandler<BotHandler>()
    .RegisterHandler<ManageHandler>();

// Register services
botClient.RegisterContainers(x =>
{
    x.RegisterType<TickerService>()
        .As<ITickerService>()
        .SingleInstance();

    x.RegisterType<TxtWordsDataset>()
        .WithParameter("data", Resources.BadWordsDataset
            .Split("\n")
            .Select(x => x.Replace("\r", "").ToLower())
        )
        .Keyed<TxtWordsDataset>(Consts.BadWordsService)
        .SingleInstance();

    x.RegisterType<TxtWordsDataset>()
        .WithParameter("data", Resources.Advices
            .Split("\n")
        )
        .Keyed<TxtWordsDataset>(Consts.AdvicesService)
        .SingleInstance();

    x.RegisterType<UserManager>()
       .As<IUserManager>()
       .InstancePerLifetimeScope();

    x.RegisterType<CatService>()
        .As<ICatService>()
        .OwnedByLifetimeScope();

    x.RegisterType<DuplicateDetectorService>()
       .As<IDuplicateDetectorService>()
       .WithParameter("expireTime", TimeSpan.FromDays(5))
       .SingleInstance();

    x.RegisterType<ChatSettingsService>()
        .As<IChatSettingsService>()
        .SingleInstance();
    
    x.RegisterType<TeslaChatCounterService>()
        .As<ITeslaChatCounterService>()
        .SingleInstance();
});

botClient.Build();

await botClient.StartReceiving();

// Wait for eternity
await Task.Delay(-1);
