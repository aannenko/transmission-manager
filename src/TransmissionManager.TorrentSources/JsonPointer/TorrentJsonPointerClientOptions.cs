using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.JsonPointer;

/// <summary>
/// Configures how a JSON source is fetched, read under a memory bound, and turned into a magnet link.
/// </summary>
/// <remarks>
/// Paired with <see cref="ValidateTorrentJsonPointerClientOptions"/>, which is the only thing that
/// checks any of this and where each setting's accepted values and their reasons are written.
/// </remarks>
public sealed class TorrentJsonPointerClientOptions
{
    private readonly Lazy<Regex?> _lazyDefaultJsonValueRegex;
    private readonly Lazy<CompositeFormat?> _lazyDefaultJsonValueFormat;

    /// <summary>Initializes JSON-pointer source options.</summary>
    public TorrentJsonPointerClientOptions()
    {
        _lazyDefaultJsonValueRegex = new(() => string.IsNullOrEmpty(DefaultJsonValueRegexPattern)
            ? null
            : RegexUtils.CreateCompiledRegex(DefaultJsonValueRegexPattern, RegexMatchTimeout));

        _lazyDefaultJsonValueFormat = new(() => string.IsNullOrEmpty(DefaultJsonValueFormat)
            ? null
            : CompositeFormat.Parse(DefaultJsonValueFormat));
    }

    /// <summary>Gets or sets the response-body read timeout.</summary>
    /// <returns>The response-body read timeout.</returns>
    public required TimeSpan ResponseReadTimeout { get; set; }

    /// <summary>
    /// The buffer a JSON document is read through, and so the largest single token it may hold - a
    /// value of up to three bytes less, once its quotes and closing delimiter are counted.
    /// </summary>
    /// <returns>The maximum JSON token size in bytes.</returns>
    public required int MaxJsonTokenBytes { get; set; }

    /// <summary>
    /// Extracts the part of the addressed string that identifies the torrent, as its whole match.
    /// If it's empty or <c>null</c>, the whole string is used as is.
    /// </summary>
    /// <returns>The default JSON value regular expression.</returns>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? DefaultJsonValueRegexPattern { get; set; }

    /// <summary>Gets or sets the regular-expression match timeout.</summary>
    /// <returns>The match timeout.</returns>
    public required TimeSpan RegexMatchTimeout { get; set; }

    /// <summary>
    /// Builds a magnet link out of the extracted value, which <c>{0}</c> stands for.
    /// If it's empty or <c>null</c>, the extracted value is used as is.
    /// </summary>
    /// <returns>The default JSON value format.</returns>
    public string? DefaultJsonValueFormat { get; set; }

    /// <summary>Gets the compiled default JSON value regular expression.</summary>
    /// <returns>The compiled regular expression, or <see langword="null"/> when none is configured.</returns>
    public Regex? DefaultJsonValueRegex => _lazyDefaultJsonValueRegex.Value;

    /// <summary>Gets the parsed default JSON value format.</summary>
    /// <returns>The composite format, or <see langword="null"/> when none is configured.</returns>
    public CompositeFormat? DefaultJsonValueCompositeFormat => _lazyDefaultJsonValueFormat.Value;
}
