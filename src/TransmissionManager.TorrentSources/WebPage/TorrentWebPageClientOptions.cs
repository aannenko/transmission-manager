using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.WebPage;

/// <summary>
/// Configures how a web page is fetched and scanned for a magnet link.
/// </summary>
/// <remarks>
/// Paired with <see cref="ValidateTorrentWebPageClientOptions"/>, which is the only thing that
/// checks any of this and where each setting's accepted values and their reasons are written.
/// </remarks>
public sealed class TorrentWebPageClientOptions
{
    private readonly Lazy<Regex> _lazyDefaultMagnetRegex;

    /// <summary>Initializes web-page source options.</summary>
    public TorrentWebPageClientOptions()
    {
        _lazyDefaultMagnetRegex = new(() =>
            RegexUtils.CreateCompiledRegex(DefaultMagnetRegexPattern!, RegexMatchTimeout));
    }

    /// <summary>Gets or sets the response-body read timeout.</summary>
    /// <returns>The response-body read timeout.</returns>
    public required TimeSpan ResponseReadTimeout { get; set; }

    /// <summary>Gets or sets the default magnet-link regular expression.</summary>
    /// <returns>The default regular-expression pattern.</returns>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public required string DefaultMagnetRegexPattern { get; set; }

    /// <summary>Gets or sets the regular-expression match timeout.</summary>
    /// <returns>The match timeout.</returns>
    public required TimeSpan RegexMatchTimeout { get; set; }

    /// <summary>Gets the compiled default magnet-link regular expression.</summary>
    /// <returns>The compiled regular expression.</returns>
    public Regex DefaultMagnetRegex => _lazyDefaultMagnetRegex.Value;
}
