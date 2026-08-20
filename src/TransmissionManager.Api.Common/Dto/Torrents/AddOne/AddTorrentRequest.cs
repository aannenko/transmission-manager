using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class AddTorrentRequest : IValidatableObject
{
    private static readonly MagnetRegexAttribute _magnetRegex = new();

    [Required]
    [HttpUri]
    public required Uri SourceUri { get; init; }

    [EnumDataType(typeof(TorrentSourceKind))]
    public TorrentSourceKind SourceKind { get; init; }

    [Required]
    public required string DownloadDir { get; init; }

    /// <remarks>
    /// A web page pattern has to look for a magnet link; a JSON pattern is unconstrained. An
    /// attribute cannot express that, because the rule depends on the value of another property.
    /// </remarks>
    public string? MagnetRegexPattern { get; init; }

    [JsonValueFormat] // null and empty both mean the configured default
    public string? JsonValueFormat { get; init; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Cron] // null and empty both mean no schedule
    public string? Cron { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // A JSON pattern is not checked at all: its match is the value, and no amount of looking at
        // a pattern's text says whether the value it picks is the right one. An undefined kind is
        // not a web page either, so it falls out here and is reported as the bad kind it is.
        if (!string.IsNullOrEmpty(MagnetRegexPattern) &&
            SourceKind is TorrentSourceKind.WebPage &&
            !_magnetRegex.IsValid(MagnetRegexPattern))
        {
            yield return new ValidationResult(_magnetRegex.ErrorMessage, [nameof(MagnetRegexPattern)]);
        }
    }
}
