using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Describes a torrent to add.</summary>
public sealed class AddTorrentRequest : IValidatableObject
{
    /// <summary>Gets the address from which to find the magnet link.</summary>
    /// <returns>The torrent source URI.</returns>
    [Required]
    [HttpUri]
    public required Uri SourceUri { get; init; }

    /// <summary>Gets how the source is interpreted.</summary>
    /// <returns>The torrent source kind.</returns>
    [EnumDataType(typeof(TorrentSourceKind))]
    public TorrentSourceKind SourceKind { get; init; }

    /// <summary>Gets the target directory in Transmission.</summary>
    /// <returns>The download directory.</returns>
    [Required]
    public required string DownloadDir { get; init; }

    /// <summary>
    /// Finds the torrent's magnet link, or the value one is built from, in what its source returns.
    /// </summary>
    /// <remarks>
    /// Built with <c>RegexOptions.ExplicitCapture</c>, so a plain <c>(…)</c> only groups and
    /// captures nothing; name a group to capture or backreference it.
    /// <para>
    /// Its remaining rules depend on the source kind, so they cannot be attributes here.
    /// </para>
    /// </remarks>
    /// <returns>The optional magnet-link regular expression.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MaxLength(TorrentSourceRules.MaxPatternLength)]
    public string? MagnetRegexPattern { get; init; }

    /// <summary>Gets the optional format that turns an extracted JSON value into a magnet link.</summary>
    /// <returns>The JSON value format.</returns>
    [JsonValueFormat] // null and empty both mean the configured default
    public string? JsonValueFormat { get; init; }

    /// <summary>Gets the optional refresh schedule.</summary>
    /// <returns>The five-field cron expression.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Cron] // null and empty both mean no schedule
    public string? Cron { get; init; }

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        TorrentSourceRules.GetValidationResults(SourceKind, MagnetRegexPattern, JsonValueFormat);
}
