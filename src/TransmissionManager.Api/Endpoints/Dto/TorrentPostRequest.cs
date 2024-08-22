using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "MinLengthAttribute in this case will access string.Length which does not get not trimmed")]
    public string? MagnetRegexPattern { get; set; }

    [MinLength(1, ErrorMessage = $"The property {nameof(Cron)} cannot be an empty string.")]
    [RegularExpression(EndpointsRegex.IsCron, MatchTimeoutInMilliseconds = 50)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "MinLengthAttribute in this case will access string.Length which does not get not trimmed")]
    public string? Cron { get; set; }
}
