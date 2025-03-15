using System.Diagnostics.CodeAnalysis;

namespace YeOldeLinkDetector.Bot;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class ConfigurationService()
{
  private readonly string? _token = Environment.GetEnvironmentVariable("TOKEN");
  public string Token
  {
    get =>
      string.IsNullOrWhiteSpace(_token)
        ? throw new InvalidOperationException("Missing TOKEN environment variable.")
        : _token;
  }
}
