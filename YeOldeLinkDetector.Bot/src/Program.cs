using System.Globalization;
using Serilog;
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
  .AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger());

builder.Services
  .AddDbContext<DataContext>()
  .AddHostedService<DiscordWorker>(sp => new DiscordWorker(TOKEN, sp.GetRequiredService<DataContext>()));

using var cts = new CancellationTokenSource();

using var host = builder.Build();
await host.RunAsync(cts.Token).ConfigureAwait(false);

