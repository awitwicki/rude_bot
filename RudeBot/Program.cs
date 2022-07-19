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

// Register services
botClient.RegisterContainers(x =>
{
    x.RegisterType<TickerService>()
        .As<ITickerService>()
        .SingleInstance();

    x.RegisterType<TxtWordsDatasetReader>()
        .WithParameter("path", Consts.BadWordsTxtPath)
        .Keyed<TxtWordsDatasetReader>(Consts.BadWordsReaderService)
        .SingleInstance();

    x.RegisterType<TxtWordsDatasetReader>()
        .WithParameter("path", Consts.AdvicesTxtPath)
        .Keyed<TxtWordsDatasetReader>(Consts.AdvicesReaderService)
        .SingleInstance();

    x.RegisterType<UserManager>()
       .As<IUserManager>()
       .WithParameter("path", Consts.AdvicesTxtPath)
       .InstancePerLifetimeScope();

    x.RegisterType<CatService>()
        .As<ICatService>()
        .OwnedByLifetimeScope();

    x.RegisterType<DuplicateDetectorService>()
       .As<IDuplicateDetectorService>()
       .WithParameter("expireTime", TimeSpan.FromDays(5))
       .WithParameter("gain", 0.9)
       .SingleInstance();
});

botClient.Build();

botClient.StartReveiving();

// Create database if not exists
using (DataContext _dbContext = new DataContext())
{
    _dbContext.Database.Migrate();
}

// Wait for eternity
await Task.Delay(Int32.MaxValue);
