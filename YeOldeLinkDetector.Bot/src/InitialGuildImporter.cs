using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

internal static class InitialGuildImporter
{
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

  public static async Task Import(SocketGuild guild)
  {
    using var db = new DataContext();
    foreach (var channel in guild.TextChannels)
    {
      try
      {
        Console.WriteLine($"    processing messages for guild ({guild.Name}) - channel ({channel.Name})");
        var added = 0;
        await foreach (var message in GetAllNonEmptyNonBotMessagesAsync(channel).ConfigureAwait(false))
        {
          foreach (var url in FindUrlsInContent.FindUrls(message.Content))
          {
            var existing = await db.Messages.FirstOrDefaultAsync(e => e.MessageId == message.Id.ToString(CultureInfo.InvariantCulture) && e.Url == url).ConfigureAwait(false);
            if (existing != null)
            {
              Console.WriteLine($"  Stopping load for guild/channel ({guild.Name} // {channel.Name}) as message with ID + URL already exists: {message.Id} - {url}");
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
              Console.WriteLine($"    Added {added} messages for channel: {channel.Name} ({channel.Id})");
            }
          }
        }
        await db.SaveChangesAsync().ConfigureAwait(false);
        Console.WriteLine("  No more messages for channel: " + channel.Name + " (" + channel.Id + ")");
      }
      catch (Discord.Net.HttpException e)
      {
        if (((int)e.HttpCode) == 50001)
        {
          Console.WriteLine("  Missing permission to read channel " + channel.Name + " (" + channel.Id + ")");
        }
      }
    }
  }
}
