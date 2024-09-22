using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Options;

namespace TransmissionManager.TorrentWebPages.Services;

public sealed class TorrentWebPageClient(IOptionsMonitor<TorrentWebPageClientOptions> options, HttpClient httpClient)
{
    public async Task<string?> FindMagnetUriAsync(
        string torrentWebPageUri,
        string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        var regex = GetMagnetSearchRegexWithValidation(regexPattern);

        using var stream = await httpClient.GetStreamAsync(torrentWebPageUri, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        var nextLineTask = reader.ReadLineAsync(cancellationToken);
        string? line;
        while ((line = await nextLineTask.ConfigureAwait(false)) is not null)
        {
            nextLineTask = reader.ReadLineAsync(cancellationToken);
            var match = regex.Match(line);
            if (match.Success && match.Groups.TryGetValue(TorrentRegex.MagnetGroup, out var group))
                return group.Value;
        }

        return null;
    }

    private Regex GetMagnetSearchRegexWithValidation(string? regexPattern)
    {
        if (regexPattern is null)
            return GetMagnetSearchRegex(
                options.CurrentValue.DefaultMagnetRegexPattern,
                options.CurrentValue.RegexMatchTimeoutMilliseconds);

        if (!TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
            throw new ArgumentException(
                $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.",
                nameof(regexPattern));

        return GetMagnetSearchRegex(regexPattern, options.CurrentValue.RegexMatchTimeoutMilliseconds);
    }

    private static Regex GetMagnetSearchRegex(string regexPattern, int timeoutMilliseconds)
    {
        return new(
            regexPattern,
            RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(timeoutMilliseconds));
    }
}
