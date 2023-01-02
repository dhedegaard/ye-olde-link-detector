using Discord.WebSocket;

var TOKEN = Environment.GetEnvironmentVariable("TOKEN");
if (string.IsNullOrWhiteSpace(TOKEN))
{
  throw new Exception("Missing TOKEN environment variable.");
}

var client = new DiscordSocketClient();

client.Log += msg =>
{
  Console.WriteLine(msg.ToString());
  return Task.CompletedTask;
};

client.MessageReceived += msg =>
{
  if (msg.Content == "!ping")
  {
    msg.Channel.SendMessageAsync("pong");
  }
  return Task.CompletedTask;
};

client.CurrentUserUpdated += (oldUser, newUser) =>
{
  Console.WriteLine($"User {oldUser.Username}#{oldUser.Discriminator} changed their username to {newUser.Username}#{newUser.Discriminator}");
  return Task.CompletedTask;
};

client.GuildAvailable += guild =>
{
  Console.WriteLine($"Guild \"{guild.Name}\" is available ({guild.TextChannels.Count} channels).");
  return Task.CompletedTask;
};

client.Connected += async () =>
{
  await client.SetActivityAsync(new Discord.Game("Waiting for illegal links", Discord.ActivityType.CustomStatus));
  Console.WriteLine("Connected");
};

await client.LoginAsync(Discord.TokenType.Bot, TOKEN, true);
await client.StartAsync();
await Task.Delay(-1);