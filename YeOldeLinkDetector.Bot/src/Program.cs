using System.Globalization;
using Serilog;
using YeOldeLinkDetector.Bot;
using YeOldeLinkDetector.Data;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging
  .ClearProviders()
  .AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger());

builder.Services
  .AddDbContext<DataContext>()
  .AddScoped<ConfigurationService>()
  .AddScoped<InitialGuildImporter>()
  .AddHostedService<DiscordWorker>();

using var cts = new CancellationTokenSource();

using var host = builder.Build();
await host.RunAsync(cts.Token).ConfigureAwait(false);

