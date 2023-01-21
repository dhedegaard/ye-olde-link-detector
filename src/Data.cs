using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace YeOldeLinkDetector
{
  public class DataContext : DbContext
  {
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
        // .LogTo(Console.WriteLine)
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
