using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.TorrentWebPages.Constants;

namespace TransmissionManager.Api.Actions.UpdateTorrentById;

internal sealed class UpdateTorrentByIdRequest
{
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    [MinLength(1)]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    public string? DownloadDir { get; init; }

    [RegularExpression(TorrentRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? MagnetRegexPattern { get; init; }

    [RegularExpression(EndpointRegex.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? Cron { get; init; }
}
