using Discord.WebSocket;

var TOKEN = Environment.GetEnvironmentVariable("TOKEN");
if (string.IsNullOrWhiteSpace(TOKEN))
{
  throw new Exception("Missing TOKEN environment variable.");
}

using var db = new DataContext();

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
    });
  }
  return Task.CompletedTask;
};

client.GuildAvailable += (guild) =>
{
  InitialGuildImporter.Import(guild, db);
  return Task.CompletedTask;
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
