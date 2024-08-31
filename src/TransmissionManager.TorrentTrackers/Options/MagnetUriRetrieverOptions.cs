using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Trackers.Constants;

namespace TransmissionManager.TorrentTrackers.Options;

public sealed class MagnetUriRetrieverOptions
{
    [Required]
    [RegularExpression(TrackersRegex.IsFindMagnet)]
    public required string DefaultRegexPattern { get; set; }

    [Required]
    [Range(50, 1000)]
    public required int RegexMatchTimeoutMilliseconds { get; set; }
}
