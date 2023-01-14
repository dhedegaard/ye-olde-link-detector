
using Discord.WebSocket;

public static class InitialGuildImporter
{
  public static void Import(SocketGuild guild, DataContext db)
  {
    // TODO: Handle messages in all the chunks or whatever.
    foreach (var channel in guild.TextChannels)
    {
      _ = Task.Run(async () =>
      {
        try
        {
          await foreach (var chunk in channel.GetMessagesAsync())
          {
            Console.WriteLine($"  processing chunk for guild ({guild.Name}) - channel ({channel.Name}) - chunk: {chunk.Count}");
            foreach (var message in chunk)
            {
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
            }
            await db.SaveChangesAsync();
          }
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
