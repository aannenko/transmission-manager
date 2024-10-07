using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Options;
using TransmissionManager.TorrentWebPages.Utils;

namespace TransmissionManager.TorrentWebPages.Services;

public sealed class TorrentWebPageClient(IOptionsMonitor<TorrentWebPageClientOptions> options, HttpClient httpClient)
{
    public async Task<string?> FindMagnetUriAsync(
        Uri torrentWebPageUri,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? regexPattern = null,
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
            return options.CurrentValue.DefaultMagnetRegex.Value;

        if (!TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
            throw new ArgumentException(
                $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.",
                nameof(regexPattern));

        return RegexUtils.CreateRegex(regexPattern, options.CurrentValue.RegexMatchTimeout);
    }
}
