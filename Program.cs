using System.Text.RegularExpressions;
using Discord.WebSocket;

var TOKEN = Environment.GetEnvironmentVariable("TOKEN");
if (string.IsNullOrWhiteSpace(TOKEN))
{
  throw new Exception("Missing TOKEN environment variable.");
}

static IEnumerable<string> FindUrlsInContent(string content)
{
  if (string.IsNullOrWhiteSpace(content))
  {
    return Enumerable.Empty<string>();
  }
  var result = UrlRegex().Match(content);
  return from e in result.Captures
         select e.Value;
}

using var client = new DiscordSocketClient(new DiscordSocketConfig
{
  GatewayIntents = Discord.GatewayIntents.Guilds | Discord.GatewayIntents.GuildMessages
});


client.MessageReceived += msg =>
{
  if (msg.Content == "!ping")
  {
    msg.Channel.SendMessageAsync("pong");
  }
  return Task.CompletedTask;
};

client.GuildAvailable += async (guild) =>
{
  Console.WriteLine($"Guild \"{guild.Name}\" is available ({guild.Channels.Count} channels).");
  foreach (var channel in guild.TextChannels)
  {
    _ = Task.Run(async () =>
    {
      try
      {
        await foreach (var chunk in channel.GetMessagesAsync())
        {
          foreach (var message in chunk)
          {
            if (message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
            {
              continue;
            }
            var urls = FindUrlsInContent(message.Content);
            if (urls.Any())
            {
              Console.WriteLine("URLS:" + string.Join(", ", urls));
              throw new Exception("STOP!");
            }
          }
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
};

client.Connected += async () =>
{
  await client.SetActivityAsync(new Discord.Game("Waiting for illegal links", Discord.ActivityType.CustomStatus));
  Console.WriteLine("Connected");
};
client.Disconnected += (ex) =>
{
  Console.WriteLine("Disconnected, rethrowing exception");
  throw ex;
};

await client.LoginAsync(Discord.TokenType.Bot, TOKEN, true);
await client.StartAsync();

await Task.Delay(-1);

partial class Program
{
  [GeneratedRegex("(?:(?:(?:https?|ftp):)?\\/\\/)(?:\\S+(?::\\S*)?@)?(?:(?!(?:10|127)(?:\\.\\d{1,3}){3})(?!(?:169\\.254|192\\.168)(?:\\.\\d{1,3}){2})(?!172\\.(?:1[6-9]|2\\d|3[0-1])(?:\\.\\d{1,3}){2})(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}(?:\\.(?:[1-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))|(?:(?:[a-z0-9\\u00a1-\\uffff][a-z0-9\\u00a1-\\uffff_-]{0,62})?[a-z0-9\\u00a1-\\uffff]\\.)+(?:[a-z\\u00a1-\\uffff]{2,}\\.?))(?::\\d{2,5})?(?:[/?#]\\S*)?", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
  private static partial Regex UrlRegex();
}
