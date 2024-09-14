using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Options;

namespace TransmissionManager.TorrentWebPages.Services;

public sealed partial class TorrentWebPageClient(
    IOptionsMonitor<TorrentWebPageServiceOptions> options,
    HttpClient httpClient)
{
    public async Task<string?> FindMagnetUriAsync(
        string torrentWebPageUri,
        string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        var regex = GetMagnetSearchRegexWithValidation(regexPattern);

        using var stream = await httpClient.GetStreamAsync(torrentWebPageUri, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            var match = regex.Match(line);
            if (match.Success && match.Groups.TryGetValue(TorrentRegex.MagnetGroup, out var group))
                return group.Value;
        }

        return null;
    }

    private Regex GetMagnetSearchRegexWithValidation(string? regexPattern)
    {
        string finalRegexPattern;
        if (regexPattern is null)
        {
            finalRegexPattern = options.CurrentValue.DefaultMagnetRegexPattern;
            if (finalRegexPattern is null || !TorrentRegex.IsFindMagnetRegex().IsMatch(finalRegexPattern))
            {
                throw new InvalidOperationException(
                    $"Invalid {nameof(options.CurrentValue.DefaultMagnetRegexPattern)} config value. " +
                    $"The value must match '{TorrentRegex.IsFindMagnet}'.");
            }
        }
        else
        {
            finalRegexPattern = regexPattern;
            if (!TorrentRegex.IsFindMagnetRegex().IsMatch(finalRegexPattern))
            {
                throw new ArgumentException(
                    $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.",
                    nameof(regexPattern));
            }
        }

        return new(
            finalRegexPattern,
            RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(options.CurrentValue.RegexMatchTimeoutMilliseconds));
    }
}
