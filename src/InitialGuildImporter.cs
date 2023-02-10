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
              foreach (var message in chunk)
              {
                messageIds.Add(message.Id);
                hasAtLeastOneMessage = true;
                if (message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
                {
                  continue;
                }
                foreach (var url in FindUrlsInContent.FindUrls(message.Content))
                {
                  var existing = await db.Messages.FirstOrDefaultAsync(e => e.MessageId == message.Id.ToString() && e.Url == url);
                  if (existing != null)
                  {
                    Console.WriteLine($"  Stopping load for guild/channel ({guild.Name} // {channel.Name}) as message with ID + URL already exists: {message.Id} - {url}");
                    return;
                  }
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
              }
              await db.SaveChangesAsync();

              var lowestMessageId = messageIds.Min();
              lastMessageId = lastMessageId.HasValue && lastMessageId.Value == lowestMessageId
                ? null
                : lowestMessageId;
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
