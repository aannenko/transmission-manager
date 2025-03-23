using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Constants;
using TransmissionManager.TorrentWebPages.Constants;

namespace TransmissionManager.Api.Actions.Torrents.Add;

internal sealed class AddTorrentRequest
{
    [Required]
    public required Uri WebPageUri { get; init; }

    [Required]
    public required string DownloadDir { get; init; }

    [RegularExpression(TorrentRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)]
    public string? MagnetRegexPattern { get; init; }

    [RegularExpression(EndpointRegex.IsCron, MatchTimeoutInMilliseconds = 50)]
    public string? Cron { get; init; }
}
