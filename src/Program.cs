using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using YeOldeLinkDetector;

var TOKEN = Environment.GetEnvironmentVariable("TOKEN");
if (string.IsNullOrWhiteSpace(TOKEN))
{
  throw new Exception("Missing TOKEN environment variable.");
}

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
      using var db = new DataContext();
      var existing = await db.Messages
        .Where(e => e.Url == url && e.ChannelId == msg.Channel.Id.ToString())
        .OrderBy(e => e.Timestamp)
        .ToListAsync();
      if (existing.Any())
      {
        reply = Formatter.FormatOutputMessage(
          userId: msg.Author.Id.ToString(),
          url: url,
          postCount: existing.Count,
          firstTimePosted: existing.First());

      }
      await db.AddAsync(
        new Message(
          MessageId: msg.Id.ToString(),
          Url: url,
          ChannelId: msg.Channel.Id.ToString(),
          Timestamp: msg.CreatedAt,
          AuthorName: msg.Author.Username
        )
      );
      await db.SaveChangesAsync();
      if (!string.IsNullOrWhiteSpace(reply))
      {
        await msg.Channel.SendMessageAsync(text: reply);
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
}

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
