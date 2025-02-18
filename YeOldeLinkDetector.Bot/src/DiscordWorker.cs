using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

internal sealed class DiscordWorker(string Token, DataContext db) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {

    using var client = new DiscordSocketClient(new DiscordSocketConfig
    {
      GatewayIntents = Discord.GatewayIntents.Guilds
        | Discord.GatewayIntents.GuildMessages
        | Discord.GatewayIntents.MessageContent,
    });

    client.Log += (msg) =>
    {
      Console.WriteLine($"Discord.Net LOG: {msg}");
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
            Console.WriteLine("REPLY: " + reply);
          }
        });
      }
      return Task.CompletedTask;
    };

    client.GuildAvailable += (guild) =>
    {
      _ = Task.Run(() => InitialGuildImporter.Import(guild));
      return Task.CompletedTask;
    };

    client.Connected += async () =>
    {
      await client.SetActivityAsync(new Discord.Game("Waiting for illegal links", Discord.ActivityType.CustomStatus)).ConfigureAwait(false);
      Console.WriteLine("Connected");
    };
    client.Disconnected += (ex) =>
    {
      Console.WriteLine("Disconnected, rethrowing exception");
      throw ex;
    };

    await client.LoginAsync(Discord.TokenType.Bot, Token, true).ConfigureAwait(false);
    await client.StartAsync().ConfigureAwait(false);

    await Task.Delay(-1, stoppingToken).ConfigureAwait(false);
  }
}
