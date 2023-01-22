using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace YeOldeLinkDetector
{
  public class DataContext : DbContext
  {
    private static readonly object dataContextLock = new();
    public static dynamic DataContextLock => dataContextLock;

    public DbSet<Message> Messages { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      var dataDirectory = Path.Join(Directory.GetCurrentDirectory(), "data");
      if (!Directory.Exists(dataDirectory))
      {
        Directory.CreateDirectory(dataDirectory);
      }
      optionsBuilder
        .UseSqlite(new SqliteConnectionStringBuilder
        {
          DataSource = Path.Join(dataDirectory, "data.sqlite"),
        }.ToString())
        .EnableDetailedErrors()
        .LogTo(Console.WriteLine, LogLevel.Warning)
        .EnableThreadSafetyChecks();
    }
  }


  [Index(nameof(Url), nameof(ChannelId), nameof(Timestamp))]
  public record Message(
    [property: Key]
  string MessageId,
    string Url,
    string ChannelId,
    DateTimeOffset Timestamp,
    string AuthorName
  );
}
