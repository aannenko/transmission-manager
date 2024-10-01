using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.TorrentWebPages.Constants;

namespace TransmissionManager.Api.UpdateTorrentById;

public sealed class UpdateTorrentByIdRequest
{
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "MinLengthAttribute in this case will access string.Length which does not get not trimmed")]
    public string? DownloadDir { get; init; }

    [RegularExpression(TorrentRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? MagnetRegexPattern { get; init; }

    [RegularExpression(EndpointRegex.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? Cron { get; init; }
}
