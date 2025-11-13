using YeOldeLinkDetector.Bot;
using YeOldeLinkDetector.Data;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder
  .Logging.ClearProviders()
  .SetMinimumLevel(level: LogLevel.Debug)
  .AddSimpleConsole(options =>
  {
    options.IncludeScopes = false;
    options.SingleLine = true;
    options.UseUtcTimestamp = true;
  });

builder
  .Services.AddDbContextFactory<DataContext>()
  .AddScoped<ConfigurationService>()
  .AddScoped<InitialGuildImporter>()
  .AddHostedService<DiscordWorker>();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) => cts.Cancel();

using var host = builder.Build();

while (!cts.IsCancellationRequested)
{
  await host.RunAsync(cts.Token).ConfigureAwait(false);
}
