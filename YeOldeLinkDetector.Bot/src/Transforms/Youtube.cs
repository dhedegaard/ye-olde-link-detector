using System.Web;

namespace YeOldeLinkDetector.Bot.Transforms;

internal static class YoutubeTransform
{
  internal static string Transform(string urlStr)
  {
    // Skip all non-youtube related domains.
    if (!urlStr.Contains("youtu.be/", StringComparison.InvariantCultureIgnoreCase) && !urlStr.Contains("youtube.com/watch?", StringComparison.InvariantCultureIgnoreCase))
    {
      return urlStr;
    }
    var urlObj = new UriBuilder(urlStr);
    var queryObj = HttpUtility.ParseQueryString(urlObj.Query);

    // Convert youtu.be -> youtube.com, move the videoId from the path to a GET
    // parameter.
    if (urlObj.Host == "youtu.be")
    {
      urlObj.Host = "www.youtube.com";
      var videoId = string.Concat(urlObj.Path.Skip(1));
      urlObj.Path = "/watch";
      queryObj.Set("v", videoId);
      urlObj.Query = queryObj.ToString();
    }

    // Implicitly, remove any GET parameters not being "v".
    queryObj.AllKeys
      .Where(e => e != "v")
      .ToList()
      .ForEach(e => queryObj.Remove(e));

    urlObj.Query = queryObj.ToString();
    // NOTE: This is sort of a hack, but setting the port to -1 removes it
    // from the output string.
    urlObj.Port = -1;
    return urlObj.ToString();
  }
}
