using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.WebPage;

/// <remarks>
/// Paired with <see cref="ValidateTorrentWebPageClientOptions"/>, which is the only thing that
/// checks any of this and where each setting's accepted values and their reasons are written.
/// </remarks>
public sealed class TorrentWebPageClientOptions
{
    private readonly Lazy<Regex> _lazyDefaultMagnetRegex;

    public TorrentWebPageClientOptions()
    {
        _lazyDefaultMagnetRegex = new(() =>
            RegexUtils.CreateCompiledRegex(DefaultMagnetRegexPattern!, RegexMatchTimeout));
    }

    public required TimeSpan ResponseReadTimeout { get; set; }

    [StringSyntax(StringSyntaxAttribute.Regex)]
    public required string DefaultMagnetRegexPattern { get; set; }

    public required TimeSpan RegexMatchTimeout { get; set; }

    public Regex DefaultMagnetRegex => _lazyDefaultMagnetRegex.Value;
}
