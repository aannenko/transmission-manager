using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Validation;

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
    /// Built with <c>RegexOptions.ExplicitCapture</c>, so a plain <c>(…)</c> only groups and
    /// captures nothing; name a group to capture or backreference it.
    /// <para>
    /// Its remaining rules depend on the source kind, which this request does not carry, so they are
    /// checked against the stored torrent.
    /// </para>
    /// </remarks>
    // null is ignored, empty string nullifies existing value
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MaxLength(TorrentSourceRules.MaxPatternLength)]
    public string? MagnetRegexPattern { get; init; }

    /// <remarks>
    /// Only its shape is checked here; whether the torrent reads a format at all depends on the
    /// source kind and is checked against the stored torrent.
    /// </remarks>
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
