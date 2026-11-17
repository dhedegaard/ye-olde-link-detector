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

No test suite; verify with `dotnet build` (baseline: 0 warnings, 0 errors; `.editorconfig` marks `Migrations/` as generated code so analyzers skip it). CI: `dotnet.yml` (restore + build) and `docker-publish.yml`.

- The bot never migrates at startup, and `./data` can lag behind. Before `docker compose up bot`, run `docker compose run --rm -T bot dotnet-ef database update --project YeOldeLinkDetector.Data`. Use `dotnet-ef`, not `dotnet ef`: the image copies the local gitignored `.config/dotnet-tools.json`, so `dotnet ef` asks for a tool restore. The compose `db` publishes no port.

## Environment variables

- `TOKEN` — Discord bot token (required at startup)
  - The `.envrc` `TOKEN` is the production bot's. A local run is a second live instance (real servers get double replies), so keep runs short and stop them afterwards.
- `CONNECTION_STRING` — Postgres connection string (defaults to `localhost` if unset)
- To test against a fresh DB without touching `./data`: `docker run --rm -p 127.0.0.1:<port>:5432 -e POSTGRES_PASSWORD=x postgres:18-alpine`, point `CONNECTION_STRING` at it, then `dotnet ef database update --project YeOldeLinkDetector.Data`.

## Key patterns

- **DB scoping**: always `await using var db = await dbFactory.CreateDbContextAsync(ct)` — never inject `DataContext` directly; use `IDbContextFactory<DataContext>`.
- **Logging**: use `LoggerMessage.Define` static delegates, not inline string interpolation. Placeholders bind by position, not by name, so the generic argument order must match the template's.
- **Event handlers**: Discord event handlers must return synchronously; wrap async work in `Task.Run(async () => { ... })`.
- **New URL transforms**: add a static class in `Transforms/` and call it from `FindUrlsInContent.FindUrls`.
- **New services**: register in `Program.cs`; prefer `AddScoped` for DB-touching services.
- **Scratch scripts**: a .NET 10 single-file app (`dotnet run x.cs` with `#:project …/YeOldeLinkDetector.Data.csproj`) needs `#:property PublishAot=false`. Otherwise EF throws "Model building is not supported when publishing with NativeAOT".
- **GatewayIntents**: `Guilds | GuildMessages | MessageContent` — changing these requires matching Discord bot permission changes.
