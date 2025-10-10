using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class UpdateTorrentByIdRequest
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)] // null is ignored, empty string is invalid
    public string? DownloadDir { get; init; }

    [MagnetRegex] // null is ignored, empty string nullifies existing value
    public string? MagnetRegexPattern { get; init; }

    [Cron] // null is ignored, empty string nullifies existing value
    public string? Cron { get; init; }
}
