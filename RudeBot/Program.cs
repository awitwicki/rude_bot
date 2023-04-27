// See https://aka.ms/new-console-template for more information
using Autofac;
using Microsoft.EntityFrameworkCore;
using PowerBot.Lite;
using RudeBot;
using RudeBot.Database;
using RudeBot.Managers;
using RudeBot.Services;
using RudeBot.Services.DuplicateDetectorService;

Console.WriteLine("Starting RudeBot");

string botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

// Run bot
CoreBot botClient = new CoreBot(botToken);

// Create database if not exists
await using (DataContext dbContext = new DataContext())
{
    dbContext.Database.Migrate();
    Console.WriteLine("Database is synchronized");
}

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
});

botClient.Build();

await botClient.StartReveiving();

// Wait for eternity
await Task.Delay(-1);
