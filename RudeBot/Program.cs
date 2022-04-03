// See https://aka.ms/new-console-template for more information
using Autofac;
using PowerBot.Lite;
using RudeBot.Database;
using RudeBot.Services;

Console.WriteLine("Starting RudeBot");

string botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

// Create database if not exists
DataContext _dbContext = new DataContext();
_dbContext.Database.EnsureCreated();
_dbContext.Dispose();

// Create DI container
var builder = new ContainerBuilder();

// Register services
builder.RegisterType<TickerService>().As<ITickerService>().SingleInstance();

// Build container
DIContainerInstance.Container = builder.Build();

// Run bot
CoreBot botClient = new CoreBot(botToken);

// Wait for eternity
await Task.Delay(Int32.MaxValue);
