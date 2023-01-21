using Discord.WebSocket;

namespace YeOldeLinkDetector
{
  public static class InitialGuildImporter
  {
    public static void Import(SocketGuild guild)
    {
      // TODO: Handle messages in all the chunks or whatever.
      foreach (var channel in guild.TextChannels)
      {
        _ = Task.Run(async () =>
        {
          try
          {
            ulong? lastMessageId = null;
            var hasAtLeastOneMessage = false;
            do
            {
              await foreach (var chunk in
                lastMessageId == null || !lastMessageId.HasValue
                  ? channel.GetMessagesAsync(limit: 1000)
                  : channel.GetMessagesAsync(limit: 1000, dir: Discord.Direction.Before, fromMessageId: lastMessageId.Value)
              )
              {
                Console.WriteLine($"  processing chunk for guild ({guild.Name}) - channel ({channel.Name}) - chunk: {chunk.Count} - lastMessageId: {lastMessageId}");
                // TODO: If all the messages in the chunk is already known,
                // stop fetching chunks as we probably have all the messages.
                using var db = new DataContext();
                foreach (var message in chunk)
                {
                  hasAtLeastOneMessage = true;
                  if (message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
                  {
                    continue;
                  }
                  foreach (var url in FindUrlsInContent.FindUrls(message.Content))
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
                  lastMessageId = message.Id;
                }
                await db.SaveChangesAsync();
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

        });
      }
    }
  }
}
