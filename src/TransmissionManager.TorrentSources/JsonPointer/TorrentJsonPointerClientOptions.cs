using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.JsonPointer;

/// <remarks>
/// Paired with <see cref="ValidateTorrentJsonPointerClientOptions"/>, which is the only thing that
/// checks any of this and where each setting's accepted values and their reasons are written.
/// </remarks>
public sealed class TorrentJsonPointerClientOptions
{
    private readonly Lazy<Regex?> _lazyDefaultJsonValueRegex;
    private readonly Lazy<CompositeFormat?> _lazyDefaultJsonValueFormat;

    public TorrentJsonPointerClientOptions()
    {
        _lazyDefaultJsonValueRegex = new(() => string.IsNullOrEmpty(DefaultJsonValueRegexPattern)
            ? null
            : RegexUtils.CreateCompiledRegex(DefaultJsonValueRegexPattern, RegexMatchTimeout));

        _lazyDefaultJsonValueFormat = new(() => string.IsNullOrEmpty(DefaultJsonValueFormat)
            ? null
            : CompositeFormat.Parse(DefaultJsonValueFormat));
    }

    public required TimeSpan ResponseReadTimeout { get; set; }

    /// <summary>
    /// The buffer a JSON document is read through, and so the largest single token it may hold - a
    /// value of up to three bytes less, once its quotes and closing delimiter are counted.
    /// </summary>
    public required int MaxJsonTokenBytes { get; set; }

    /// <summary>
    /// Extracts the part of the addressed string that identifies the torrent, as its whole match.
    /// If it's empty or <c>null</c>, the whole string is used as is.
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? DefaultJsonValueRegexPattern { get; set; }

    public required TimeSpan RegexMatchTimeout { get; set; }

    /// <summary>
    /// Builds a magnet link out of the extracted value, which <c>{0}</c> stands for.
    /// If it's empty or <c>null</c>, the extracted value is used as is.
    /// </summary>
    public string? DefaultJsonValueFormat { get; set; }

    public Regex? DefaultJsonValueRegex => _lazyDefaultJsonValueRegex.Value;

    public CompositeFormat? DefaultJsonValueCompositeFormat => _lazyDefaultJsonValueFormat.Value;
}
