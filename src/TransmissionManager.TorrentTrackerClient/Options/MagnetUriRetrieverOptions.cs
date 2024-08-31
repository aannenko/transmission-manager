using System.ComponentModel.DataAnnotations;
using TransmissionManager.TorrentTrackerClient.Constants;

namespace TransmissionManager.TorrentTrackerClient.Options;

public sealed class MagnetUriRetrieverOptions
{
    [Required]
    [RegularExpression(TrackersRegex.IsFindMagnet)]
    public required string DefaultRegexPattern { get; set; }

    [Required]
    [Range(50, 1000)]
    public required int RegexMatchTimeoutMilliseconds { get; set; }
}
