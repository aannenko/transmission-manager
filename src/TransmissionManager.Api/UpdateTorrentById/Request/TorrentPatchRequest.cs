using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.TorrentWebPages.Constants;

namespace TransmissionManager.Api.UpdateTorrentById.Request;

public sealed class TorrentPatchRequest
{
    [RegularExpression(TorrentRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are considered valid
    public string? MagnetRegexPattern { get; set; }

    [RegularExpression(EndpointRegex.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are considered valid
    public string? Cron { get; set; }
}
