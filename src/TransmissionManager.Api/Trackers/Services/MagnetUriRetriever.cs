using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TransmissionManager.Api.Trackers.Options;
using TransmissionManager.Api.Utilities;

namespace TransmissionManager.Api.Trackers.Services;

public sealed partial class MagnetUriRetriever(
    IOptionsMonitor<MagnetUriRetrieverOptions> options,
    HttpClient httpClient)
{
    public async Task<string?> FindMagnetUri(
        string trackerWebPageUri,
        string? regexPattern,
        CancellationToken cancellationToken = default)
    {
        var regex = GetMagnetSearchRegexWithValidation(regexPattern);

        using var stream = await httpClient.GetStreamAsync(trackerWebPageUri, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            var match = regex.Match(line);
            if (match.Success && match.Groups.TryGetValue(AppRegex.FindMagnetGroup, out var group))
                return group.Value;
        }

        return null;
    }

    private Regex GetMagnetSearchRegexWithValidation(string? regexPattern)
    {
        string finalRegexPattern;
        if (regexPattern is null)
        {
            finalRegexPattern = options.CurrentValue.DefaultRegexPattern;
            if (finalRegexPattern is null || !AppRegex.FindMagnetRegex().IsMatch(finalRegexPattern))
                throw new InvalidOperationException(
                    $"Invalid {nameof(options.CurrentValue.DefaultRegexPattern)} config value. " +
                    $"The value must match '{AppRegex.FindMagnet}'.");
        }
        else
        {
            finalRegexPattern = regexPattern;
            if (!AppRegex.FindMagnetRegex().IsMatch(finalRegexPattern))
                throw new ArgumentException(
                    $"Invalid magnet-matching regex provided. The value must match '{AppRegex.FindMagnet}'.",
                    nameof(regexPattern));
        }

        return new(
            finalRegexPattern,
            RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(options.CurrentValue.RegexMatchTimeoutMilliseconds));
    }
}
