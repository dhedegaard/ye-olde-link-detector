namespace YeOldeLinkDetector.Bot;

internal sealed class ConfigurationService()
{
  private readonly string? _token = Environment.GetEnvironmentVariable("TOKEN");
  public string Token
  {
    get
    {
      if (string.IsNullOrWhiteSpace(_token))
      {
        throw new InvalidOperationException("Missing TOKEN environment variable.");
      }
      return _token;
    }
  }
}
