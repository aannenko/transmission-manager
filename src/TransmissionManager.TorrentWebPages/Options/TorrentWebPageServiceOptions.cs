using System.ComponentModel.DataAnnotations;
using TransmissionManager.TorrentWebPages.Constants;

namespace TransmissionManager.TorrentWebPages.Options;

public sealed class TorrentWebPageServiceOptions
{
    [Required]
    [RegularExpression(TorrentRegex.IsFindMagnet)]
    public required string DefaultMagnetRegexPattern { get; set; }

    [Required]
    [Range(10, 500)]
    public required int RegexMatchTimeoutMilliseconds { get; set; }
}
