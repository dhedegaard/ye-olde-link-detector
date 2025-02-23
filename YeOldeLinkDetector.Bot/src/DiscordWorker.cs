using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class DiscordWorker(ILogger<DiscordWorker> logger, ConfigurationService configurationService, IDbContextFactory<DataContext> dbFactory, InitialGuildImporter initialGuildImporter) : BackgroundService
{
  private static readonly Action<ILogger, string, Exception?> _logDiscordNet =
      LoggerMessage.Define<string>(LogLevel.Debug, new EventId(0, "DiscordNet"), "Discord.Net LOG: {Message}");
  private static readonly Action<ILogger, string, Exception?> _logReply =
      LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "Reply"), "REPLY: {Reply}");
  private static readonly Action<ILogger, Exception?> _logConnected =
      LoggerMessage.Define(LogLevel.Information, new EventId(2, "Connection"), "Connected");
  private static readonly Action<ILogger, Exception> _logDisconnectError =
      LoggerMessage.Define(LogLevel.Error, new EventId(3, "DisconnectError"), "Disconnected unexpectedly, rethrowing exception");
  private static readonly Action<ILogger, Exception?> _logShutdown =
      LoggerMessage.Define(LogLevel.Debug, new EventId(4, "Shutdown"), "Disconnected due to shutdown");

  [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var client = new DiscordSocketClient(new DiscordSocketConfig
    {
      GatewayIntents = Discord.GatewayIntents.Guilds
        | Discord.GatewayIntents.GuildMessages
        | Discord.GatewayIntents.MessageContent,
    });

    // Register cancellation token to stop the client
    stoppingToken.Register(() =>
    {
      _ = client.StopAsync();
    });

    client.Log += (msg) =>
    {
      _logDiscordNet(logger, msg.ToString(), null);
      return Task.CompletedTask;
    };
    client.MessageReceived += msg =>
    {
      if (msg.Author.IsBot)
      {
        return Task.CompletedTask;
      }
      foreach (var url in FindUrlsInContent.FindUrls(msg.Content))
      {
        _ = Task.Run(async () =>
        {
          await using var db = await dbFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
          string reply = "";
          var existing = (await db.Messages
            .Where(e => e.Url == url && e.ChannelId == msg.Channel.Id.ToString(CultureInfo.InvariantCulture))
            .OrderBy(e => e.Timestamp)
            .ToListAsync().ConfigureAwait(false))
            .AsReadOnly();
          if (existing.Count != 0)
          {
            reply = Formatter.FormatOutputMessage(
              userId: msg.Author.Id.ToString(CultureInfo.InvariantCulture),
              url: url,
              postCount: existing.Count,
              firstTimePosted: existing[0]);
          }
          await db.AddAsync(
            new Message(
              MessageId: msg.Id.ToString(CultureInfo.InvariantCulture),
              Url: url,
              ChannelId: msg.Channel.Id.ToString(CultureInfo.InvariantCulture),
              Timestamp: msg.CreatedAt,
              AuthorName: msg.Author.Username
            )
          ).ConfigureAwait(false);
          await db.SaveChangesAsync().ConfigureAwait(false);
          if (!string.IsNullOrWhiteSpace(reply))
          {
            await msg.Channel.SendMessageAsync(text: reply).ConfigureAwait(false);
            _logReply(logger, reply, null);
          }
        });
      }
      return Task.CompletedTask;
    };

    client.GuildAvailable += (guild) =>
    {
      _ = Task.Run(() => initialGuildImporter.Import(guild));
      return Task.CompletedTask;
    };

    client.Connected += async () =>
    {
      await client.SetActivityAsync(new Discord.Game("Waiting for illegal links", Discord.ActivityType.CustomStatus)).ConfigureAwait(false);
      _logConnected(logger, null);
    };
    client.Disconnected += (ex) =>
    {
      if (!stoppingToken.IsCancellationRequested)
      {
        _logDisconnectError(logger, ex);
        throw ex;
      }
      _logShutdown(logger, null);
      return Task.CompletedTask;
    };

    await client.LoginAsync(
      tokenType: Discord.TokenType.Bot,
      token: configurationService.Token,
      validateToken: true).ConfigureAwait(false);
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await client.StartAsync().ConfigureAwait(false);
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
      }
      catch (TaskCanceledException) { }
    }

    // Graceful shutdown
    await client.StopAsync().ConfigureAwait(false);
  }
}
