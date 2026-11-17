using System.Text.RegularExpressions;
using YeOldeLinkDetector.Bot.Transforms;

namespace YeOldeLinkDetector.Bot;

internal static partial class FindUrlsInContent
{
  [GeneratedRegex(
    "(?:(?:(?:https?|ftp):)?\\/\\/)(?:\\S+(?::\\S*)?@)?(?:(?!(?:10|127)(?:\\.\\d{1,3}){3})(?!(?:169\\.254|192\\.168)(?:\\.\\d{1,3}){2})(?!172\\.(?:1[6-9]|2\\d|3[0-1])(?:\\.\\d{1,3}){2})(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}(?:\\.(?:[1-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))|(?:(?:[a-z0-9\\u00a1-\\uffff][a-z0-9\\u00a1-\\uffff_-]{0,62})?[a-z0-9\\u00a1-\\uffff]\\.)+(?:[a-z\\u00a1-\\uffff]{2,}\\.?))(?::\\d{2,5})?(?:[/?#]\\S*)?",
    RegexOptions.IgnoreCase,
    matchTimeoutMilliseconds: 1000
  )]
  private static partial Regex UrlRegex();

  internal static IReadOnlyList<string> FindUrls(string content)
  {
    if (string.IsNullOrWhiteSpace(content))
    {
      return [];
    }
    try
    {
      // Materialize inside the try: Matches() is lazy, so a timeout would
      // otherwise surface in the caller's foreach.
      return
      [
        .. (
          from match in UrlRegex().Matches(content)
          select match.Value into value
          where !string.IsNullOrWhiteSpace(value)
          select YoutubeTransform.Transform(value)
        ).Distinct(StringComparer.Ordinal),
      ];
    }
    catch (RegexMatchTimeoutException)
    {
      // Crafted input can make the regex backtrack for a long time; treat the
      // message as having no URLs rather than stalling the bot.
      return [];
    }
  }
}
