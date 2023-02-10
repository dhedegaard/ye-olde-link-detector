using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace YeOldeLinkDetector
{
  public static class InitialGuildImporter
  {
    public static async Task Import(SocketGuild guild)
    {
      using var db = new DataContext();
      foreach (var channel in guild.TextChannels)
      {
        var guildElem = await db.Guilds
          .Where(e => e.GuildId == guild.Id.ToString())
          .FirstOrDefaultAsync();

        try
        {
          ulong? lastMessageId = null;
          var hasAtLeastOneMessage = false;
          do
          {
            Console.WriteLine($"  processing message chunks for guild ({guild.Name}) - channel ({channel.Name}) - lastMessageId: {lastMessageId}");
            var messageIds = new HashSet<ulong>();
            await foreach (var chunk in
              lastMessageId == null || !lastMessageId.HasValue
                ? channel.GetMessagesAsync()
                : channel.GetMessagesAsync(dir: Discord.Direction.Before, fromMessageId: lastMessageId.Value)
            )
            {
              // TODO: If all the messages in the chunk is already known,
              // stop fetching chunks as we probably have all the messages.
              foreach (var message in chunk)
              {
                if (guildElem != null && message.Id.ToString() == guildElem.LastMessageId)
                {
                  Console.WriteLine($"  hit LastMessageId for guild: {guild.Name}");
                  return;
                }

                // If there's no guild element, or the message is newer than the last message
                // we've seen, update the guild element for the next boot.
                if (guildElem == null || guildElem.LastMessageTimestamp < message.CreatedAt)
                {
                  guildElem = new Guild(
                    GuildId: guild.Id.ToString(),
                    LastMessageId: message.Id.ToString(),
                    LastMessageTimestamp: message.CreatedAt);
                  await db.AddAsync(guildElem);
                }

                messageIds.Add(message.Id);
                hasAtLeastOneMessage = true;
                if (message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
                {
                  continue;
                }
                foreach (var url in FindUrlsInContent.FindUrls(message.Content))
                {
                  var existing = await db.FindAsync<Message>(message.Id.ToString());
                  if (existing == null)
                  {
                    await db.AddAsync(
                      new Message(
                        MessageId: message.Id.ToString(),
                        Url: url,
                        ChannelId: message.Channel.Id.ToString(),
                        Timestamp: message.CreatedAt,
                        AuthorName: message.Author.Username
                      )
                    );
                  }
                  await db.SaveChangesAsync();
                }
              }
              var lowestMessageId = messageIds.Min();
              lastMessageId = lastMessageId.HasValue && lastMessageId.Value == lowestMessageId
                ? null
                : lowestMessageId;
              Console.WriteLine($"  done processing chunk for guild ({guild.Name}) - channel ({channel.Name}) - chunk: {chunk.Count} - lastMessageId: {lastMessageId}");
            }
          } while (hasAtLeastOneMessage && lastMessageId.HasValue);
          Console.WriteLine("no more messages for channel: " + channel.Name + " (" + channel.Id + ")");
        }
        catch (Discord.Net.HttpException e)
        {
          if (((int)e.HttpCode) == 50001)
          {
            Console.WriteLine("Missing permission to read channel " + channel.Name + " (" + channel.Id + ")");
          }
        }

      }
    }
  }
}
