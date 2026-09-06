using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>
/// A partial update to a torrent, carrying only the fields it changes.
/// </summary>
/// <remarks>
/// A <see langword="null"/> field is left alone and an empty one clears the stored value, so a
/// request carrying nothing at all asks for no change and is refused. <see cref="DownloadDir"/> is
/// the exception: a torrent cannot be without one, so it refuses an empty string rather than
/// clearing.
/// </remarks>
public sealed class UpdateTorrentByIdRequest : IValidatableObject
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)]
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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MaxLength(TorrentSourceRules.MaxPatternLength)]
    public string? MagnetRegexPattern { get; init; }

    /// <summary>
    /// Builds the torrent's magnet link out of the value its pattern extracts from a JSON source.
    /// </summary>
    /// <remarks>
    /// Only its shape is checked here; whether the torrent reads a format at all depends on the
    /// source kind and is checked against the stored torrent.
    /// </remarks>
    [JsonValueFormat]
    public string? JsonValueFormat { get; init; }

    [Cron]
    public string? Cron { get; init; }

    /// <summary>
    /// Refuses an update that would change nothing.
    /// </summary>
    /// <param name="validationContext">The context this validation runs in.</param>
    /// <returns>One failure keyed to the request, or nothing if any field is present.</returns>
    /// <remarks>
    /// The emptiness of the body is what is wrong, so it is reported against the request rather than
    /// against the fields it could have carried - naming those would say each of them is invalid.
    /// </remarks>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DownloadDir is null && MagnetRegexPattern is null && JsonValueFormat is null && Cron is null)
            yield return new ValidationResult("At least one field must be provided.", [ProblemDetailsKeys.Request]);
    }
}
