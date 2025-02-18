using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

internal sealed class InitialGuildImporter(ILogger<InitialGuildImporter> logger, DataContext db)
{
  private static readonly Action<ILogger, string, string, Exception?> _logProcessingChannel =
      LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(0, "Processing"),
      "    processing messages for guild ({GuildName}) - channel ({ChannelName})");

  private static readonly Action<ILogger, string, string, string, string, Exception?> _logDuplicateFound =
      LoggerMessage.Define<string, string, string, string>(LogLevel.Information, new EventId(1, "Duplicate"),
      "  Stopping load for guild/channel ({GuildName} // {ChannelName}) as message with ID + URL already exists: {MessageId} - {Url}");

  private static readonly Action<ILogger, int, string, ulong, Exception?> _logProgress =
      LoggerMessage.Define<int, string, ulong>(LogLevel.Information, new EventId(2, "Progress"),
      "    Added {Added} messages for channel: {ChannelName} ({ChannelId})");

  private static readonly Action<ILogger, string, ulong, Exception?> _logChannelComplete =
      LoggerMessage.Define<string, ulong>(LogLevel.Information, new EventId(3, "Complete"),
      "  No more messages for channel: {ChannelName} ({ChannelId})");

  private static readonly Action<ILogger, string, ulong, Exception?> _logMissingPermission =
      LoggerMessage.Define<string, ulong>(LogLevel.Information, new EventId(4, "Permission"),
      "  Missing permission to read channel {ChannelName} ({ChannelId})");

  private static async IAsyncEnumerable<Discord.IMessage> GetAllNonEmptyNonBotMessagesAsync(SocketTextChannel channel)
  {
    var hasAtLeastOneMessage = false;
    ulong? lastMessageId = null;
    do
    {
      await foreach (var chunk in
        (lastMessageId == null || !lastMessageId.HasValue
          ? channel.GetMessagesAsync()
          : channel.GetMessagesAsync(fromMessageId: lastMessageId.Value, dir: Discord.Direction.Before)).ConfigureAwait(false)
      )
      {
        var messages = chunk
          .Where(message => !message.Author.IsBot && !string.IsNullOrWhiteSpace(message.Content))
          .ToList()
          .AsReadOnly();
        hasAtLeastOneMessage = messages.Count != 0;

        if (messages.Count == 0)
        {
          // No messages in the current chunk, stop fetching messages for
          // the given channel.
          yield break;
        }

        foreach (var message in messages)
        {
          yield return message;
        }

        var lowestMessageId = messages.Min(message => message.Id);
        lastMessageId = lastMessageId == lowestMessageId
          ? null
          : lowestMessageId;
      }
    } while (hasAtLeastOneMessage && lastMessageId.HasValue);
  }

  public async Task Import(SocketGuild guild)
  {
    foreach (var channel in guild.TextChannels)
    {
      try
      {
        _logProcessingChannel(logger, guild.Name, channel.Name, null);
        var added = 0;
        await foreach (var message in GetAllNonEmptyNonBotMessagesAsync(channel).ConfigureAwait(false))
        {
          foreach (var url in FindUrlsInContent.FindUrls(message.Content))
          {
            var existing = await db.Messages.FirstOrDefaultAsync(e => e.MessageId == message.Id.ToString(CultureInfo.InvariantCulture) && e.Url == url).ConfigureAwait(false);
            if (existing != null)
            {
              _logDuplicateFound(logger, guild.Name, channel.Name, message.Id.ToString(), url, null);
              return;
            }
            await db.AddAsync(
              new Message(
                MessageId: message.Id.ToString(CultureInfo.InvariantCulture),
                Url: url,
                ChannelId: message.Channel.Id.ToString(CultureInfo.InvariantCulture),
                Timestamp: message.CreatedAt,
                AuthorName: message.Author.Username
              )
            ).ConfigureAwait(false);
            added++;
            if (added % 50 == 0)
            {
              _logProgress(logger, added, channel.Name, channel.Id, null);
            }
          }
        }
        await db.SaveChangesAsync().ConfigureAwait(false);
        _logChannelComplete(logger, channel.Name, channel.Id, null);
      }
      catch (Discord.Net.HttpException e)
      {
        if (((int)e.HttpCode) == 50001)
        {
          _logMissingPermission(logger, channel.Name, channel.Id, null);
        }
      }
    }
  }
}
