using System.ComponentModel.DataAnnotations;
using TransmissionManager.TorrentTrackerClient.Constants;

namespace TransmissionManager.TorrentTrackerClient.Options;

public sealed class TorrentWebPageServiceOptions
{
    [Required]
    [RegularExpression(TrackersRegex.IsFindMagnet)]
    public required string DefaultMagnetRegexPattern { get; set; }

    [Required]
    [Range(10, 500)]
    public required int RegexMatchTimeoutMilliseconds { get; set; }
}
