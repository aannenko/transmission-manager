using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Common.Validation;

/// <summary>
/// Checks the settings a torrent uses to find its magnet link.
/// </summary>
/// <remarks>
/// These cannot be attributes on the properties they check, because the rules depend on the source
/// kind - and an update request does not carry one, so only the stored torrent knows it.
/// </remarks>
public static class TorrentSourceRules
{
    /// <summary>
    /// The longest pattern accepted, in characters.
    /// </summary>
    /// <remarks>
    /// Limits how long checking a pattern can take, not what it can express - a real magnet search
    /// is a few dozen characters. Building a regular expression takes longer the longer its pattern
    /// is, and cannot be cancelled: measured ~0.06 ms at this cap, ~16 ms at 64 KB, ~14 s at 2.7 MB.
    /// </remarks>
    public const int MaxPatternLength = 512;

    private const string _jsonValueFormatUnused = "A magnet format is only used by a JsonPointer source.";

    /// <remarks>
    /// Must match the options the source clients build a pattern with, because the options decide
    /// what parses - otherwise a pattern accepted here can still be refused when it is used.
    /// </remarks>
    private const RegexOptions _patternOptions = RegexOptions.ExplicitCapture;

    private static readonly MagnetRegexAttribute _magnetRegex = new();

    /// <summary>
    /// Checks a torrent's magnet regex and magnet format against its source kind.
    /// </summary>
    /// <param name="sourceKind">Decides which rules apply.</param>
    /// <param name="magnetRegexPattern">The torrent's magnet regex, if it has one.</param>
    /// <param name="jsonValueFormat">The torrent's magnet format, if it has one.</param>
    /// <returns>
    /// An array of field-name-to-errors-array pairs, or an empty array if there is nothing wrong.
    /// </returns>
    /// <remarks>
    /// Under any source kind, the rest of the parameters is allowed to be null or empty.
    /// </remarks>
    public static KeyValuePair<string, string[]>[] Validate(
        TorrentSourceKind sourceKind,
        string? magnetRegexPattern,
        string? jsonValueFormat)
    {
        var (patternError, formatError) = GetErrors(sourceKind, magnetRegexPattern, jsonValueFormat);

        return (patternError, formatError) switch
        {
            (null, null) => [],
            (not null, null) => [new(nameof(AddTorrentRequest.MagnetRegexPattern), [patternError])],
            (null, not null) => [new(nameof(AddTorrentRequest.JsonValueFormat), [formatError])],
            _ =>
            [
                new(nameof(AddTorrentRequest.MagnetRegexPattern), [patternError]),
                new(nameof(AddTorrentRequest.JsonValueFormat), [formatError]),
            ],
        };
    }

    /// <summary>
    /// Does the same as <see cref="Validate"/>, but returns the results in the form
    /// <see cref="IValidatableObject"/> expects.
    /// </summary>
    /// <returns>One result per error message, naming the field it belongs to.</returns>
    /// <remarks>
    /// Builds its results directly rather than reshaping what <see cref="Validate"/> returns, which
    /// would allocate the other shape only to throw it away - measured 80 B against 192 B for one
    /// error.
    /// </remarks>
    public static IEnumerable<ValidationResult> GetValidationResults(
        TorrentSourceKind sourceKind,
        string? magnetRegexPattern,
        string? jsonValueFormat)
    {
        var (patternError, formatError) = GetErrors(sourceKind, magnetRegexPattern, jsonValueFormat);

        return (patternError, formatError) switch
        {
            (null, null) => [],
            (not null, null) => [new(patternError, [nameof(AddTorrentRequest.MagnetRegexPattern)])],
            (null, not null) => [new(formatError, [nameof(AddTorrentRequest.JsonValueFormat)])],
            _ =>
            [
                new(patternError, [nameof(AddTorrentRequest.MagnetRegexPattern)]),
                new(formatError, [nameof(AddTorrentRequest.JsonValueFormat)]),
            ],
        };
    }

    /// <returns>
    /// What is wrong with each setting, or <see langword="null"/> for a setting that is fine.
    /// </returns>
    private static (string? PatternError, string? FormatError) GetErrors(
        TorrentSourceKind sourceKind,
        string? magnetRegexPattern,
        string? jsonValueFormat)
    {
        // A web page source never reads a format, and a torrent cannot change its kind after it is created,
        // so a format stored with a web page source would stay unused for as long as the torrent exists.
        var formatError = sourceKind is TorrentSourceKind.WebPage && !string.IsNullOrEmpty(jsonValueFormat)
            ? _jsonValueFormatUnused
            : null;

        return (GetPatternError(sourceKind, magnetRegexPattern), formatError);
    }

    /// <returns>
    /// What is wrong with <paramref name="pattern"/>, or <see langword="null"/> if nothing is.
    /// </returns>
    private static string? GetPatternError(TorrentSourceKind sourceKind, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return null;

        // If it does not parse, that is the only thing worth saying - telling its author what it
        // should have searched for is no help while it is not a regular expression at all.
        var parseError = GetPatternParseError(pattern);
        if (parseError is not null)
            return parseError;

        return sourceKind is TorrentSourceKind.WebPage && !_magnetRegex.IsValid(pattern)
            ? _magnetRegex.ErrorMessage!
            : null;
    }

    /// <returns>
    /// Why <paramref name="pattern"/> is not a regular expression, or <see langword="null"/> if it is.
    /// </returns>
    /// <remarks>
    /// Building the pattern is the only way to find out, and the result is thrown away because the
    /// source clients build their own with a match timeout this project cannot see.
    /// </remarks>
    private static string? GetPatternParseError(string pattern)
    {
        try
        {
            _ = new Regex(pattern, _patternOptions);
            return null;
        }
        catch (RegexParseException e)
        {
            return e.Message;
        }
    }
}
