# RudeBot

Telegram karma bot for @rude_chat community. Tracks user reputation, detects profanity, generates AI-powered daily chat digests.

## Tech Stack

- .NET 8.0 / C# 13
- PostgreSQL via EF Core 9 (Npgsql)
- Telegram.Bot 22.3 + PowerBot.Lite 2.2.5 (bot framework)
- Autofac (DI)
- Google Gemini API (chat digest summaries)
- Cron.NET (scheduled tasks)
- Docker (multi-stage build, docker-compose)

## Solution Structure

```
RudeBot/              Main application (entry point, handlers, services, models, migrations)
RudeBot.Domain/       Shared constants, interfaces, resources (bad words, advices)
RudeBot.Common/       Shared utilities and transaction helpers
RudeBot.Tests/        Unit tests (xUnit + NSubstitute)
RudeBot.Common.Tests/ Common library tests
DatabaseChatMigrator/ CLI tool for importing Telegram JSON chat exports
```

## Key Architecture

- **Entry point**: `RudeBot/Program.cs` — sets up Autofac DI, EF Core, registers middleware/handlers, starts polling
- **Middleware**: `BotMiddleware` — processes every message (bad words tracking, user stats, allowed chats whitelist, chat context recording)
- **Handlers**: `BotHandler` (karma, advice, help, stats commands), `ManageHandler` (welcome flow, auth callbacks)
- **Database**: `RudeBot/Database/DataContext.cs` — EF Core context with `Users`, `UserStats`, `ChatSettings`, `TeslaChatCounters`
- **Services**: UserManager, ChatSettingsService, ChatDigestService, ChatContextService, DuplicateDetectorService, TeslaChatCounterService

## Environment Variables

- `RUDEBOT_TELEGRAM_TOKEN` — Bot token (required)
- `RUDEBOT_DB_CONNECTION_STRING` — PostgreSQL connection string (required)
- `RUDEBOT_GEMINI_API_KEY` — Google Gemini API key
- `RUDEBOT_GEMINI_MODEL_NAME` — Gemini model name
- `RUDEBOT_FLOOD_TIMEOUT` — Rate limiting in seconds (default: 30)
- `RUDEBOT_DELETE_TIMEOUT` — Auto-delete bot messages in seconds (default: 60)
- `RUDEBOT_ALLOWED_CHATS` — Comma-separated whitelist of allowed chat IDs

## Build & Run

```bash
# Build
dotnet build RudeBot.sln

# Run tests
dotnet test RudeBot.sln

# Docker
docker-compose up --build
```

## EF Core Migrations

Migrations are in `RudeBot/Migrations/`. To add a new migration, uncomment the parameterless constructor and `OnConfiguring` in `DataContext.cs`, then:

```bash
cd RudeBot
dotnet ef migrations add <MigrationName>
```

The app auto-applies migrations on startup via `Database.MigrateAsync()`.

## Conventions

- Language: Ukrainian primary for user-facing strings, English for code
- Bot version tracked in `RudeBot.Domain/Consts.cs`
- Resources (bad words list, advices) are in `RudeBot.Domain/FileResources/` and loaded as embedded resources via `RudeBot.Domain/Resources/Resources.cs`
- Autofac DI lifetimes: SingleInstance for stateful services (caches, digest), InstancePerLifetimeScope for DB-bound services

## Rules for Claude

- NEVER commit changes unless I explicitly ask you to
- Do not commit plans or specs to the repo
- Do not use git add or git commit automatically
