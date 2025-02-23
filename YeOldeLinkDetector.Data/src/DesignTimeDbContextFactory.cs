using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace YeOldeLinkDetector.Data;


public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DataContext>
{
  public DataContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
    optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING"));
#pragma warning disable CA2000 // Dispose objects before losing scope
    return new DataContext(new Logger<DataContext>(new LoggerFactory()));
#pragma warning restore CA2000 // Dispose objects before losing scope
  }
}
