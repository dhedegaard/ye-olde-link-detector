using Humanizer;
using YeOldeLinkDetector.Data;

namespace YeOldeLinkDetector.Bot;

internal static class Formatter
{
  internal static string FormatOutputMessage(
    string userId,
    string url,
    int postCount,
    Message firstTimePosted
  ) =>
    $@"
    🚨🚨🚨**OLD**🚨🚨🚨: <@!{userId}> The URL: <{url}> has previously been posted **{postCount}** time(s) before. 🚨🚨🚨 It was first posted by **{firstTimePosted.AuthorName}**, **{firstTimePosted.Timestamp.Humanize()}** ago".Trim();
}
