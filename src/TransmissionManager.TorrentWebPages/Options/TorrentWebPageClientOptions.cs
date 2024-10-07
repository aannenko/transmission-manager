using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Utils;

namespace TransmissionManager.TorrentWebPages.Options;

public sealed class TorrentWebPageClientOptions
{
    private readonly Lazy<Regex> _lazyDefaultMagnetRegex;

    public TorrentWebPageClientOptions() =>
        _lazyDefaultMagnetRegex = new(() => RegexUtils.CreateRegex(DefaultMagnetRegexPattern!, RegexMatchTimeout));

    [StringSyntax(StringSyntaxAttribute.Regex)]
    [Required]
    [RegularExpression(TorrentRegex.IsFindMagnet)]
    public required string DefaultMagnetRegexPattern { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Required]
    [Range(typeof(TimeSpan), "00:00:00.01", "00:00:00.5")]
    public required TimeSpan RegexMatchTimeout { get; set; }

    public Regex DefaultMagnetRegex => _lazyDefaultMagnetRegex.Value;
}
