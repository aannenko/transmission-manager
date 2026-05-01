using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class UpdateTorrentByIdRequest : IValidatableObject
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)] // null is ignored, empty string is invalid
    public string? DownloadDir { get; init; }

    [MagnetRegex] // null is ignored, empty string nullifies existing value
    public string? MagnetRegexPattern { get; init; }

    [Cron] // null is ignored, empty string nullifies existing value
    public string? Cron { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DownloadDir is null && MagnetRegexPattern is null && Cron is null)
        {
            yield return new ValidationResult(
                "At least one field must be provided.",
                [nameof(DownloadDir), nameof(MagnetRegexPattern), nameof(Cron)]);
        }
    }
}
