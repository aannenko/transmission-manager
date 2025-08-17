using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class AddTorrentRequest
{
    [Required]
    public required Uri WebPageUri { get; init; }

    [Required]
    public required string DownloadDir { get; init; }

    [MagnetRegex]
    public string? MagnetRegexPattern { get; init; }

    [Cron]
    public string? Cron { get; init; }
}
