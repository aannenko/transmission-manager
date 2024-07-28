using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Utilities;

namespace TransmissionManager.Api.Endpoints.Dto;

public sealed class TorrentPutRequest
{
    public long? TransmissionId { get; set; }

    public string? DownloadDir { get; set; }

    [RegularExpression(AppRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are considered valid
    public string? MagnetRegexPattern { get; set; }

    [RegularExpression(AppRegex.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are considered valid
    public string? Cron { get; set; }
}
