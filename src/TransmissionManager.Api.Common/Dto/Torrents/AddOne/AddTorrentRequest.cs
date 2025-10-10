using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class AddTorrentRequest
{
    [Required]
    public required Uri WebPageUri { get; init; }

    [Required]
    public required string DownloadDir { get; init; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)] // null is ok, empty string is not valid
    [MagnetRegex]
    public string? MagnetRegexPattern { get; init; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)] // null is ok, empty string is not valid
    [Cron]
    public string? Cron { get; init; }
}
