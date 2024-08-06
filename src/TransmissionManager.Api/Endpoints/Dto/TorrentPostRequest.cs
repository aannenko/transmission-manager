using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Endpoints.Constants;
using TransmissionManager.Api.Trackers.Constants;

namespace TransmissionManager.Api.Endpoints.Dto;

public sealed class TorrentPostRequest
{
    [Required]
    public required string DownloadDir { get; set; }

    [Required]
    public required string WebPageUri { get; set; }

    [MinLength(1, ErrorMessage = $"The property {nameof(MagnetRegexPattern)} cannot be an empty string.")]
    [RegularExpression(TrackersRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)]
    public string? MagnetRegexPattern { get; set; }

    [MinLength(1, ErrorMessage = $"The property {nameof(Cron)} cannot be an empty string.")]
    [RegularExpression(EndpointsRegex.IsCron, MatchTimeoutInMilliseconds = 50)]
    public string? Cron { get; set; }
}
