using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Web.Forms;

internal sealed class AddTorrentForm : IValidatableObject
{
    private readonly SourceDraft _webPageDraft = new();
    private readonly SourceDraft _jsonPointerDraft = new();

    [EnumDataType(typeof(TorrentSourceKind))]
    public TorrentSourceKind SourceKind { get; private set; }

    [Required(ErrorMessage = "Value required.")]
    public string SourceUri { get; set; } = string.Empty;

    [Required(ErrorMessage = "Value required.")]
    public string DownloadDir { get; set; } = "/tvshows";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MaxLength(TorrentSourceRules.MaxPatternLength)]
    public string MagnetRegexPattern { get; set; } = string.Empty;

    [JsonValueFormat]
    public string JsonValueFormat { get; set; } = string.Empty;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Cron]
    public string Cron { get; set; } = "0 9,19 * * *";

    public bool IsWebPageSource => SourceKind is TorrentSourceKind.WebPage;

    /// <summary>
    /// Switches the form to another source kind, keeping what was typed for the previous one.
    /// </summary>
    /// <param name="sourceKind">The kind to switch to.</param>
    /// <returns>
    /// <see langword="true"/> if the form changed, <see langword="false"/> if it already held that
    /// kind.
    /// </returns>
    /// <remarks>
    /// The three fields whose meaning depends on the kind are stored per kind, so switching away and
    /// back returns what was typed; the shared fields are left alone.
    /// </remarks>
    public bool SwitchSourceKind(TorrentSourceKind sourceKind)
    {
        if (sourceKind == SourceKind)
            return false;

        SaveSourceDraft();
        SourceKind = sourceKind;
        LoadSourceDraft();
        return true;
    }

    public void Normalize()
    {
        SourceUri = SourceUri.Trim();
        DownloadDir = DownloadDir.Trim();
        MagnetRegexPattern = MagnetRegexPattern.Trim();
        JsonValueFormat = JsonValueFormat.Trim();
        Cron = Cron.Trim();
        SaveSourceDraft();
    }

    /// <summary>
    /// Builds the request this form describes.
    /// </summary>
    /// <returns>The request, carrying nothing for what the chosen kind does not read.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the source address is not an absolute http or https URI. Validation refuses that
    /// before this is reached, so it means the caller skipped validation.
    /// </exception>
    public AddTorrentRequest CreateRequest()
    {
        if (!TryCreateHttpUri(SourceUri, out var sourceUri))
            throw new InvalidOperationException($"{nameof(SourceUri)} must be validated before creating a request.");

        return new()
        {
            SourceUri = sourceUri,
            SourceKind = SourceKind,
            DownloadDir = DownloadDir,
            MagnetRegexPattern = OrNullOnEmpty(MagnetRegexPattern),
            JsonValueFormat = IsWebPageSource ? null : OrNullOnEmpty(JsonValueFormat),
            Cron = OrNullOnEmpty(Cron),
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(SourceUri) && !TryCreateHttpUri(SourceUri, out _))
        {
            yield return new ValidationResult(
                "Value must be an absolute http or https address.",
                [nameof(SourceUri)]);
        }

        var jsonValueFormat = IsWebPageSource ? null : JsonValueFormat;
        foreach (var result in TorrentSourceRules.GetValidationResults(
            SourceKind,
            MagnetRegexPattern,
            jsonValueFormat))
        {
            yield return result;
        }
    }

    private void SaveSourceDraft()
    {
        var draft = GetSourceDraft(SourceKind);
        draft.SourceUri = SourceUri;
        draft.MagnetRegexPattern = MagnetRegexPattern;
        draft.JsonValueFormat = JsonValueFormat;
    }

    private void LoadSourceDraft()
    {
        var draft = GetSourceDraft(SourceKind);
        SourceUri = draft.SourceUri;
        MagnetRegexPattern = draft.MagnetRegexPattern;
        JsonValueFormat = draft.JsonValueFormat;
    }

    private SourceDraft GetSourceDraft(TorrentSourceKind sourceKind) =>
        sourceKind switch
        {
            TorrentSourceKind.WebPage => _webPageDraft,
            TorrentSourceKind.JsonPointer => _jsonPointerDraft,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Unsupported torrent source kind."),
        };

    private static string? OrNullOnEmpty(string value) =>
        value.Length == 0 ? null : value;

    private static bool TryCreateHttpUri(string value, [NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(value, UriKind.Absolute, out uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private sealed class SourceDraft
    {
        public string SourceUri { get; set; } = string.Empty;

        public string MagnetRegexPattern { get; set; } = string.Empty;

        public string JsonValueFormat { get; set; } = string.Empty;
    }
}
