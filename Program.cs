using Discord.Net;
using Discord.WebSocket;

var TOKEN = Environment.GetEnvironmentVariable("TOKEN");
if (string.IsNullOrWhiteSpace(TOKEN))
{
  throw new Exception("Missing TOKEN environment variable.");
}

var client = new DiscordSocketClient();

client.CurrentUserUpdated += (oldUser, newUser) =>
{
  Console.WriteLine($"User {oldUser.Username}#{oldUser.Discriminator} changed their username to {newUser.Username}#{newUser.Discriminator}");
  return Task.CompletedTask;
};
client.GuildAvailable += guild =>
{
  Console.WriteLine($"Guild {guild.Name} is available.");
  return Task.CompletedTask;
};

await client.LoginAsync(Discord.TokenType.Bot, TOKEN);
await client.StartAsync();
await Task.Delay(-1);