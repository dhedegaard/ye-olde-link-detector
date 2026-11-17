using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace YeOldeLinkDetector.Data;

public class DataContext(ILogger<DataContext> logger) : DbContext
{
  public DbSet<Message> Messages { get; set; } = null!;

  private static readonly Action<ILogger, string, Exception?> _logWarning =
    LoggerMessage.Define<string>(LogLevel.Warning, new EventId(0, "EFCore"), "{Message}");

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
    optionsBuilder
      .UseNpgsql(
        Environment.GetEnvironmentVariable("CONNECTION_STRING") is string connStr
        && !string.IsNullOrWhiteSpace(connStr)
          ? connStr
          : new NpgsqlConnectionStringBuilder
          {
            ApplicationName = "ye-olde-link-detector",
            Host = "localhost",
            Database = "ye-olde-link-detector",
            IncludeErrorDetail = true,
          }.ToString()
      )
      .EnableDetailedErrors()
      .LogTo(message => _logWarning(logger, message, null), LogLevel.Warning)
      .EnableThreadSafetyChecks();
}
