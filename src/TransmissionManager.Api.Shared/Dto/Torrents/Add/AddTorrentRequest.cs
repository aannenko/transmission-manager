using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Shared.Constants;

namespace TransmissionManager.Api.Shared.Dto.Torrents.Add;

public sealed class AddTorrentRequest
{
    [Required]
    public required Uri WebPageUri { get; init; }

    [Required]
    public required string DownloadDir { get; init; }

    [RegularExpression(RegexPatterns.IsFindMagnet, MatchTimeoutInMilliseconds = 50)]
    public string? MagnetRegexPattern { get; init; }

    [RegularExpression(RegexPatterns.IsCron, MatchTimeoutInMilliseconds = 50)]
    public string? Cron { get; init; }
}
