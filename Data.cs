using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
  public DbSet<Message>? Messages { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    var dataDirectory = Path.Join(Directory.GetCurrentDirectory(), "data");
    if (!Directory.Exists(dataDirectory))
    {
      Directory.CreateDirectory(dataDirectory);
    }
    optionsBuilder.UseSqlite($"Data Source={Path.Join(dataDirectory, "data.sqlite")}");
  }
}


[Index(nameof(Url), nameof(GuildId), nameof(Timestamp))]
public record Message(
  [property: Key]
  string MessageId,
  string Url,
  string GuildId,
  DateTimeOffset Timestamp,
  string AuthorName
);
