using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class AddTorrentRequest : IValidatableObject
{
    [Required]
    [HttpUri]
    public required Uri SourceUri { get; init; }

    [EnumDataType(typeof(TorrentSourceKind))]
    public TorrentSourceKind SourceKind { get; init; }

    [Required]
    public required string DownloadDir { get; init; }

    /// <remarks>
    /// Built with <c>RegexOptions.ExplicitCapture</c>, so a plain <c>(…)</c> only groups and
    /// captures nothing; name a group to capture or backreference it.
    /// <para>
    /// Its remaining rules depend on the source kind, so they cannot be attributes here.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MaxLength(TorrentSourceRules.MaxPatternLength)]
    public string? MagnetRegexPattern { get; init; }

    [JsonValueFormat] // null and empty both mean the configured default
    public string? JsonValueFormat { get; init; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Cron] // null and empty both mean no schedule
    public string? Cron { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        TorrentSourceRules.GetValidationResults(SourceKind, MagnetRegexPattern, JsonValueFormat);
}
