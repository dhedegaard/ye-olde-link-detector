using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace YeOldeLinkDetector.Data;

public class DataContext : DbContext
{
  public DbSet<Message> Messages { get; set; } = null!;

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
    optionsBuilder
      .UseNpgsql(!string.IsNullOrWhiteSpace(connStr)
        ? connStr
        : new NpgsqlConnectionStringBuilder
        {
          ApplicationName = "ye-olde-link-detector",
          Host = "localhost",
          Database = "ye-olde-link-detector",
        }.ToString())
      .EnableDetailedErrors()
      .LogTo(Console.WriteLine, LogLevel.Warning)
      .EnableThreadSafetyChecks();
  }
}
