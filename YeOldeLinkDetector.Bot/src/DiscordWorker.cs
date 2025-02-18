using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

internal sealed class DiscordWorker(ILogger<DiscordWorker> logger, ConfigurationService configurationService, DataContext db, InitialGuildImporter initialGuildImporter) : BackgroundService
{
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
      logger.LogDebug("Discord.Net LOG: {msg}", msg);
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
            logger.LogInformation("REPLY: {Reply}", reply);
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
      logger.LogInformation("Connected");
    };
    client.Disconnected += (ex) =>
    {
      if (!stoppingToken.IsCancellationRequested)
      {
        logger.LogError(ex, "Disconnected unexpectedly, rethrowing exception");
        throw ex;
      }
      logger.LogDebug("Disconnected due to shutdown");
      return Task.CompletedTask;
    };

    await client.LoginAsync(
      tokenType: Discord.TokenType.Bot,
      token: configurationService.Token,
      validateToken: true).ConfigureAwait(false);
    await client.StartAsync().ConfigureAwait(false);

    // Wait until the token is cancelled
    try
    {
      await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      // Graceful shutdown
      await client.StopAsync().ConfigureAwait(false);
    }
  }
}
