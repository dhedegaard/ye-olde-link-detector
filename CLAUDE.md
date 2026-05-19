# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project does

A Discord bot (.NET 10 Worker Service) that detects duplicate links in Discord servers. When a message is received, it extracts and normalizes URLs, stores them in PostgreSQL, and replies if a URL has been seen before in that channel.

## Commands

```sh
# Run locally (requires TOKEN env var)
dotnet run --project YeOldeLinkDetector.Bot

# Build
dotnet build

# EF Core migrations (run from repo root)
dotnet ef migrations add <MigrationName> --project YeOldeLinkDetector.Data
dotnet ef database update --project YeOldeLinkDetector.Data

# Start local DB
docker compose up db
```

## Environment variables

- `TOKEN` — Discord bot token (required at startup)
- `CONNECTION_STRING` — Postgres connection string (defaults to `localhost` if unset)

## Architecture

Two projects in one solution:

**`YeOldeLinkDetector.Bot`** — runnable worker service
- `Program.cs` — host setup, DI registration
- `DiscordWorker.cs` — `BackgroundService` wiring Discord events; spawns `Task.Run` per URL to avoid blocking the event loop
- `FindUrlsInContent.cs` — regex-based URL extraction, feeds results through the transform pipeline
- `Transforms/Youtube.cs` — canonicalizes `youtu.be` and `youtube.com` URLs to a single `youtube.com/watch?v=...` form
- `Formatter.cs` — produces the reply message text
- `InitialGuildImporter.cs` — on `GuildAvailable`, crawls message history to bootstrap the database

**`YeOldeLinkDetector.Data`** — EF Core library
- `DataContext.cs` — Npgsql-backed `DbContext`; reads `CONNECTION_STRING` env var
- `Message.cs` — single entity record: `MessageId`, `Url`, `ChannelId`, `Timestamp`, `AuthorName`
- `Migrations/` — EF Core migration files

## Key patterns

- **DB scoping**: always `await using var db = await dbFactory.CreateDbContextAsync(ct)` — never inject `DataContext` directly; use `IDbContextFactory<DataContext>`.
- **Logging**: use `LoggerMessage.Define` static delegates, not inline string interpolation.
- **Event handlers**: Discord event handlers must return synchronously; wrap async work in `Task.Run(async () => { ... })`.
- **New URL transforms**: add a static class in `Transforms/` and call it from `FindUrlsInContent.FindUrls`.
- **New services**: register in `Program.cs`; prefer `AddScoped` for DB-touching services.
- **GatewayIntents**: `Guilds | GuildMessages | MessageContent` — changing these requires matching Discord bot permission changes.
