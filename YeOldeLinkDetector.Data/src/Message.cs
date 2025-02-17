using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace YeOldeLinkDetector.Data;

[Index(nameof(Url), nameof(ChannelId), nameof(Timestamp))]
public record Message(
  [property: Key]
  string MessageId,
#pragma warning disable CA1054 // URI-like parameters should not be strings
#pragma warning disable CA1056 // URI-like properties should not be strings
  string Url,
#pragma warning restore CA1056 // URI-like properties should not be strings
#pragma warning restore CA1054 // URI-like parameters should not be strings
  string ChannelId,
  DateTimeOffset Timestamp,
  string AuthorName
);
