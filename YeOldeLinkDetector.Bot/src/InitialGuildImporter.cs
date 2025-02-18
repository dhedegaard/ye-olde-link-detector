using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

internal sealed class InitialGuildImporter(ILogger<InitialGuildImporter> logger, DataContext db)
{
  private async IAsyncEnumerable<Discord.IMessage> GetAllNonEmptyNonBotMessagesAsync(SocketTextChannel channel)
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
        logger.LogInformation("    processing messages for guild ({GuildName}) - channel ({ChannelName})", guild.Name, channel.Name);
        var added = 0;
        await foreach (var message in GetAllNonEmptyNonBotMessagesAsync(channel).ConfigureAwait(false))
        {
          foreach (var url in FindUrlsInContent.FindUrls(message.Content))
          {
            var existing = await db.Messages.FirstOrDefaultAsync(e => e.MessageId == message.Id.ToString(CultureInfo.InvariantCulture) && e.Url == url).ConfigureAwait(false);
            if (existing != null)
            {
              logger.LogInformation("  Stopping load for guild/channel ({GuildName} // {ChannelName}) as message with ID + URL already exists: {MessageId} - {Url}", guild.Name, channel.Name, message.Id, url);
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
              logger.LogInformation("    Added {Added} messages for channel: {ChannelName} ({ChannelId})", added, channel.Name, channel.Id);
            }
          }
        }
        await db.SaveChangesAsync().ConfigureAwait(false);
        logger.LogInformation("  No more messages for channel: {ChannelName} ({ChannelId})", channel.Name, channel.Id);
      }
      catch (Discord.Net.HttpException e)
      {
        if (((int)e.HttpCode) == 50001)
        {
          logger.LogInformation("  Missing permission to read channel {ChannelName} ({ChannelId})", channel.Name, channel.Id);
        }
      }
    }
  }
}
