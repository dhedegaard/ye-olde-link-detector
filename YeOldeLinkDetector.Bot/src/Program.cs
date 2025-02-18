using System.Globalization;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using YeOldeLinkDetector.Bot;
using YeOldeLinkDetector.Data;




var TOKEN = Environment.GetEnvironmentVariable("TOKEN");
if (string.IsNullOrWhiteSpace(TOKEN))
{
  throw new InvalidOperationException("Missing TOKEN environment variable.");
}


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging
  .ClearProviders()
  .AddSimpleConsole();

builder.Services
  .AddDbContext<DataContext>()
  .AddHostedService<DiscordWorker>(sp => new DiscordWorker(TOKEN, sp.GetRequiredService<DataContext>()));

using var cts = new CancellationTokenSource();

using var host = builder.Build();
await host.RunAsync(cts.Token).ConfigureAwait(false);

