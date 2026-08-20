using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class UpdateTorrentByIdRequest : IValidatableObject
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)] // null is ignored, empty string is invalid
    public string? DownloadDir { get; init; }

    /// <summary>
    /// Finds the torrent's magnet link, or the value one is built from, in what its source returns.
    /// </summary>
    /// <remarks>
    /// Unchecked here, unlike the fields around it: what a valid pattern looks like depends on the
    /// torrent's source kind, and this request does not carry it.
    /// </remarks>
    // null is ignored, empty string nullifies existing value
    public string? MagnetRegexPattern { get; init; }

    [JsonValueFormat] // null is ignored, empty string nullifies existing value
    public string? JsonValueFormat { get; init; }

    [Cron] // null is ignored, empty string nullifies existing value
    public string? Cron { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DownloadDir is null && MagnetRegexPattern is null && JsonValueFormat is null && Cron is null)
        {
            yield return new ValidationResult(
                "At least one field must be provided.",
                [
                    nameof(DownloadDir),
                    nameof(MagnetRegexPattern),
                    nameof(JsonValueFormat),
                    nameof(Cron)
                ]);
        }
    }
}
