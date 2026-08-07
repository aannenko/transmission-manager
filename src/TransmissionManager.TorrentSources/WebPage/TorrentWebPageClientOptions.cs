using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentSources.Options;

namespace TransmissionManager.TorrentSources.WebPage;

public sealed class TorrentWebPageClientOptions : TorrentSourcesOptions
{
    private readonly Lazy<Regex> _lazyDefaultMagnetRegex;

    public TorrentWebPageClientOptions()
    {
        _lazyDefaultMagnetRegex = new(() => RegexUtils.CreateRegex(DefaultMagnetRegexPattern!, RegexMatchTimeout));
    }

    [StringSyntax(StringSyntaxAttribute.Regex)]
    [Required]
    [RegularExpression(TorrentRegex.IsFindMagnet, MatchTimeoutInMilliseconds = 50)]
    public required string DefaultMagnetRegexPattern { get; set; }

    [Required]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Range(typeof(TimeSpan), "00:00:00.01", "00:00:00.5")]
    public required TimeSpan RegexMatchTimeout { get; set; }

    public Regex DefaultMagnetRegex => _lazyDefaultMagnetRegex.Value;
}
