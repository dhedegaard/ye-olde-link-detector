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

using var host = builder.Build();

// The host's console lifetime already handles Ctrl+C and SIGTERM.
await host.RunAsync().ConfigureAwait(false);
