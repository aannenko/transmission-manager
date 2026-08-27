using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.WebPage;

/// <summary>
/// Checks <see cref="TorrentWebPageClientOptions"/> at startup, and is the only thing that does.
/// </summary>
/// <remarks>
/// The checks are ordered rather than independent: the pattern is compiled with the match timeout,
/// so that timeout has to be known good before the pattern is touched, and compiling is the only way
/// to find out whether a pattern is a pattern at all.
/// </remarks>
internal sealed class ValidateTorrentWebPageClientOptions : IValidateOptions<TorrentWebPageClientOptions>
{
    private static readonly TimeSpan _minResponseReadTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _maxResponseReadTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan _minRegexMatchTimeout = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan _maxRegexMatchTimeout = TimeSpan.FromMilliseconds(500);

    public ValidateOptionsResult Validate(string? name, TorrentWebPageClientOptions options)
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

        ValidateDefaultMagnetRegexPattern(options, isRegexMatchTimeoutValid, failures);

        return failures.Count is 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    /// <param name="isRegexMatchTimeoutValid">
    /// Whether compiling is safe to attempt. A missing setting binds to <see cref="TimeSpan.Zero"/>,
    /// which the regular expression constructor rejects with an
    /// <see cref="ArgumentOutOfRangeException"/> - leaving startup with that instead of the failure
    /// naming the setting actually at fault.
    /// </param>
    private static void ValidateDefaultMagnetRegexPattern(
        TorrentWebPageClientOptions options,
        bool isRegexMatchTimeoutValid,
        List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.DefaultMagnetRegexPattern))
        {
            failures.Add($"{nameof(options.DefaultMagnetRegexPattern)} is required.");
            return;
        }

        // Run before the other two regex checks to limit the cost of checking or compiling a long pattern.
        if (options.DefaultMagnetRegexPattern.Length > RegexUtils.MaxPatternLength)
        {
            failures.Add(
                $"{nameof(options.DefaultMagnetRegexPattern)} must be at most {RegexUtils.MaxPatternLength} " +
                $"characters, but is {options.DefaultMagnetRegexPattern.Length}.");

            return;
        }

        // Make sure the regex is an expected pattern (not necessarily valid - the next check ensures that).
        if (!TorrentRegex.IsFindMagnetRegex().IsMatch(options.DefaultMagnetRegexPattern))
        {
            failures.Add(
                $"{nameof(options.DefaultMagnetRegexPattern)} must look for a magnet link, matching " +
                $"'{TorrentRegex.IsFindMagnet}'.");

            return;
        }

        if (!isRegexMatchTimeoutValid)
            return;

        // Compile the lazy regex to fail fast if it is not a valid pattern.
        try
        {
            _ = options.DefaultMagnetRegex;
        }
        catch (RegexParseException e)
        {
            failures.Add(
                $"{nameof(options.DefaultMagnetRegexPattern)} is not a valid regular expression: {e.Message}");
        }
    }
}
