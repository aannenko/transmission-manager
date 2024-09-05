using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Endpoints.Constants;
using TransmissionManager.TorrentTrackerClient.Constants;

namespace TransmissionManager.Api.Endpoints.Dto;

public sealed class TorrentPatchRequest
{
    [RegularExpression(TrackersRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are considered valid
    public string? MagnetRegexPattern { get; set; }

    [RegularExpression(EndpointsRegex.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are considered valid
    public string? Cron { get; set; }
}
