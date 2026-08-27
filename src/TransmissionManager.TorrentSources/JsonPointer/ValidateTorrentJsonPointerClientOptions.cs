using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.JsonPointer;

/// <summary>
/// Checks <see cref="TorrentJsonPointerClientOptions"/> at startup, and is the only thing that does.
/// </summary>
/// <remarks>
/// The checks are ordered rather than independent: the pattern is compiled with the match timeout,
/// so that timeout has to be known good before the pattern is touched, and compiling is the only way
/// to find out whether a pattern is a pattern at all.
/// </remarks>
internal sealed class ValidateTorrentJsonPointerClientOptions : IValidateOptions<TorrentJsonPointerClientOptions>
{
    private const int _minJsonTokenBytes = 1024;
    private const int _maxJsonTokenBytes = 65536;

    private static readonly TimeSpan _minResponseReadTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _maxResponseReadTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan _minRegexMatchTimeout = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan _maxRegexMatchTimeout = TimeSpan.FromMilliseconds(500);

    public ValidateOptionsResult Validate(string? name, TorrentJsonPointerClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.ResponseReadTimeout < _minResponseReadTimeout ||
            options.ResponseReadTimeout > _maxResponseReadTimeout)
        {
            failures.Add(
                $"{nameof(options.ResponseReadTimeout)} must be between {_minResponseReadTimeout} and " +
                $"{_maxResponseReadTimeout}, but is {options.ResponseReadTimeout}.");
        }

        var isRegexMatchTimeoutValid =
            options.RegexMatchTimeout >= _minRegexMatchTimeout && options.RegexMatchTimeout <= _maxRegexMatchTimeout;

        if (!isRegexMatchTimeoutValid)
        {
            failures.Add(
                $"{nameof(options.RegexMatchTimeout)} must be between {_minRegexMatchTimeout} and " +
                $"{_maxRegexMatchTimeout}, but is {options.RegexMatchTimeout}.");
        }

        if (options.MaxJsonTokenBytes is < _minJsonTokenBytes or > _maxJsonTokenBytes)
        {
            failures.Add(
                $"{nameof(options.MaxJsonTokenBytes)} must be between {_minJsonTokenBytes} and " +
                $"{_maxJsonTokenBytes}, but is {options.MaxJsonTokenBytes}.");
        }

        ValidateDefaultJsonValueRegexPattern(options, isRegexMatchTimeoutValid, failures);
        ValidateDefaultJsonValueFormat(options, failures);

        return failures.Count is 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    /// <param name="isRegexMatchTimeoutValid">
    /// Whether compiling is safe to attempt. A missing setting binds to <see cref="TimeSpan.Zero"/>,
    /// which the regular expression constructor rejects with an
    /// <see cref="ArgumentOutOfRangeException"/> - leaving startup with that instead of the failure
    /// naming the setting actually at fault.
    /// </param>
    /// <remarks>
    /// No pattern at all is valid and means the whole addressed string is the value. Any pattern
    /// that compiles is accepted: whether it selects something sensible is the operator's to judge,
    /// and a wrong selection surfaces as a magnet Transmission refuses or a torrent that does not
    /// download.
    /// </remarks>
    private static void ValidateDefaultJsonValueRegexPattern(
        TorrentJsonPointerClientOptions options,
        bool isRegexMatchTimeoutValid,
        List<string> failures)
    {
        if (string.IsNullOrEmpty(options.DefaultJsonValueRegexPattern))
            return;

        // Run before the regex compilation check to limit the cost of compiling a long pattern.
        if (options.DefaultJsonValueRegexPattern.Length > RegexUtils.MaxPatternLength)
        {
            failures.Add(
                $"{nameof(options.DefaultJsonValueRegexPattern)} must be at most {RegexUtils.MaxPatternLength} " +
                $"characters, but is {options.DefaultJsonValueRegexPattern.Length}.");

            return;
        }

        if (!isRegexMatchTimeoutValid)
            return;

        // Compile the lazy regex to fail fast if it is not a valid pattern.
        try
        {
            _ = options.DefaultJsonValueRegex;
        }
        catch (RegexParseException e)
        {
            failures.Add(
                $"{nameof(options.DefaultJsonValueRegexPattern)} is not a valid regular expression: {e.Message}");
        }
    }

    /// <remarks>
    /// No format at all is valid and means the addressed string is already a magnet link.
    /// </remarks>
    private static void ValidateDefaultJsonValueFormat(
        TorrentJsonPointerClientOptions options,
        List<string> failures)
    {
        if (string.IsNullOrEmpty(options.DefaultJsonValueFormat))
            return;

        if (!JsonValueRegex.IsJsonValueFormatRegex().IsMatch(options.DefaultJsonValueFormat))
        {
            failures.Add(
                $"{nameof(options.DefaultJsonValueFormat)} must hold nothing but literal text and '{{0}}', " +
                $"matching '{JsonValueRegex.IsJsonValueFormat}'.");

            return;
        }

        _ = options.DefaultJsonValueCompositeFormat;
    }
}
