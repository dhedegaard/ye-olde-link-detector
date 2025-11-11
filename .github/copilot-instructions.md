## Purpose

This repository contains a small Discord bot (YeOldeLinkDetector) that detects duplicate links posted in servers and optionally replies when a link has been posted before. These instructions help an AI coding agent be immediately productive by explaining the big picture, the most important files, developer workflows, and project-specific conventions with concrete examples.

## Big picture (what this app does)

- A hosted .NET worker (see `Program.cs`) runs a Discord client (`DiscordSocketClient`) and listens for messages.
- When a message is received the code extracts URLs via `FindUrlsInContent.FindUrls` (see `src/FindUrlsInContent.cs`) and normalizes YouTube links with `Transforms/Youtube.cs`.
- Each discovered URL is recorded in the application's database (`YeOldeLinkDetector.Data` DataContext) and the bot may reply if the URL has been posted previously — message formatting lives in `Formatter.cs`.
- On guild availability the `InitialGuildImporter` crawls historical messages to bootstrap the database.

Key runtime components:

- `DiscordWorker` (src/DiscordWorker.cs): the background service that wires Discord events to the business logic.
- `FindUrlsInContent` (src/FindUrlsInContent.cs): URL extraction via a generated regex and a transform pipeline.
- `Transforms/Youtube.cs`: canonicalizes YouTube URLs so duplicates are easier to detect.
- `InitialGuildImporter` (src/InitialGuildImporter.cs): historical message import for a guild.

## How to build & run (developer workflows)

- Locally (simple): ensure `TOKEN` environment variable contains your bot token, then from repo root:

  dotnet run

- The project is configured as a .NET host — `Program.cs` builds a Host and registers services including `ConfigurationService`, `InitialGuildImporter`, and the `DiscordWorker` hosted service.
- There are GitHub Actions workflows in `.github/workflows/` for CI (dotnet build/test) and Docker image publishing.
- Workspace tasks (in VS Code) include `build`, `publish`, and `watch` which invoke `dotnet build`, `dotnet publish`, and `dotnet watch run` respectively.

## Environment & integration points

- TOKEN (required): bot token read by `ConfigurationService` (`Environment.GetEnvironmentVariable("TOKEN")`). Missing token throws at startup.
- Database: uses EF Core `IDbContextFactory<DataContext>` injected into services. Look for the Data project for the schema (entities like `Message`).
- Discord: uses `Discord.Net` (DiscordSocketClient) with GatewayIntents Guilds, GuildMessages and MessageContent — changes to intents require matching bot permissions.
- Docker / Postgres: repo contains `docker-compose.yml` and a `data/` directory suggesting a local Postgres data layout; use compose if you need a local DB instance that mirrors CI or production.

## Project-specific conventions & patterns (examples)

- Small single-purpose internal classes are implemented as 'file-scoped' records/functional classes and registered with DI in `Program.cs`.
- Background processing: prefer `BackgroundService` derived classes that receive dependencies via constructor injection and implement `ExecuteAsync` (see `DiscordWorker`).
- DB usage: code uses `IDbContextFactory<DataContext>` and `await using var db = await dbFactory.CreateDbContextAsync()` to scope contexts per background operation.
- Logging: uses structured `LoggerMessage.Define` static delegates (see `DiscordWorker` and `InitialGuildImporter`) rather than inline string concatenation.
- Async/cancellation: the host uses a CancellationTokenSource wired to Ctrl+C; take care to pass cancellation tokens to long-running DB or network calls when adding new code.

## Where to change common behaviours

- Add new message-processing transforms in `Transforms/` and call them from `FindUrlsInContent` or extend `FindUrlsInContent` if you need additional normalization.
- Reply content: `Formatter.FormatOutputMessage` produces the reply text — edit here for messaging changes.
- Historical import: `InitialGuildImporter.Import` handles bulk-import; inspect throttling and exception handling when modifying to avoid Discord rate limits.

## Tests, linting, and CI

- The repo relies on standard dotnet build/test in CI. Use the provided GitHub workflows in `.github/workflows/` as examples for the commands to run in CI.

## Minimal examples (concrete patterns)

- Recording a discovered URL (pattern used in `DiscordWorker.MessageReceived`):

  1. Extract URLs -> `FindUrlsInContent.FindUrls(msg.Content)`
  2. Create a new `Message` entity with `MessageId`, `Url`, `ChannelId`, `Timestamp`, `AuthorName`
  3. `await db.AddAsync(...)` + `await db.SaveChangesAsync()`
  4. Optionally `await msg.Channel.SendMessageAsync(text: reply)` after formatting

## Quick notes for an AI contributor

- Focus edits in the `YeOldeLinkDetector.Bot/src/` folder for behavior changes. Keep `Data` schema changes separate and coordinated.
- Respect the DI registration in `Program.cs`; add scoped services there when introducing new services.
- Avoid making unbounded synchronous calls in event handlers; preserve the practice of wrapping work in `Task.Run`/background tasks and use the `IDbContextFactory` pattern.

If anything here is unclear or you'd like more detail in any section (examples, DB schema, CI commands, or how to run the dockerized DB), tell me which area to expand and I will iterate.
