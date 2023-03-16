using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace YeOldeLinkDetector.Data;

[Index(nameof(Url), nameof(ChannelId), nameof(Timestamp))]
public record Message(
  [property: Key]
  string MessageId,
  string Url,
  string ChannelId,
  DateTimeOffset Timestamp,
  string AuthorName
);
