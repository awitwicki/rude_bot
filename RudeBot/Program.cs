// See https://aka.ms/new-console-template for more information
using Autofac;
using Microsoft.EntityFrameworkCore;
using PowerBot.Lite;
using RudeBot;
using RudeBot.Database;
using RudeBot.Services;

Console.WriteLine("Starting RudeBot");

string botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

// Create database if not exists
DataContext _dbContext = new DataContext();
_dbContext.Database.Migrate();
_dbContext.Dispose();

// Create DI container
var builder = new ContainerBuilder();

// Register services
builder.RegisterType<TickerService>()
    .As<ITickerService>()
    .SingleInstance();

builder.RegisterType<TxtWordsDatasetReader>()
    .Named<TxtWordsDatasetReader>(Consts.BadWordsReaderService)
    .WithParameter("path", Consts.BadWordsTxtPath)
    .SingleInstance();

builder.RegisterType<TxtWordsDatasetReader>()
    .Named<TxtWordsDatasetReader>(Consts.AdvicesReaderService)
    .WithParameter("path", Consts.AdvicesTxtPath)
    .SingleInstance();

// Build container
DIContainerInstance.Container = builder.Build();

// Run bot
CoreBot botClient = new CoreBot(botToken);

// Wait for eternity
await Task.Delay(Int32.MaxValue);
