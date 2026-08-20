using TransmissionManager.Database.Dto;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class ServiceProviderTorrentSourceExtensions
{
    /// <summary>
    /// Finds a magnet link at <paramref name="sourceUri"/>, resolving and using the client that
    /// <paramref name="sourceKind"/> selects.
    /// </summary>
    /// <param name="provider">The provider that resolves the client.</param>
    /// <param name="sourceUri">The address of the source.</param>
    /// <param name="sourceKind">Which client reads the source.</param>
    /// <param name="magnetRegexPattern">
    /// The torrent's regex pattern, or <see langword="null"/> to use the client's configured default.
    /// Every client reads it, and each means by it whatever it needs to - see the remarks.
    /// </param>
    /// <param name="jsonValueFormat">
    /// Builds a magnet link out of what a JSON source's pattern extracted, or <see langword="null"/>
    /// to use the configured default. Ignored by every other kind.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="MagnetSearchOutcome"/> representing the result of the search, including failures.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided <paramref name="sourceKind"/> is not a defined member.
    /// </exception>
    /// <remarks>
    /// A torrent carries one pattern whatever its kind, so what it has to look like depends on the
    /// client that will read it - a magnet link on a page, a value inside a JSON string. A torrent's
    /// kind is fixed when it is added, so the pattern it was written for is the one that reads it.
    /// </remarks>
    public static Task<MagnetSearchOutcome> FindMagnetUriAsync(
        this IServiceProvider provider,
        Uri sourceUri,
        TorrentSourceKind sourceKind,
        string? magnetRegexPattern = null,
        string? jsonValueFormat = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return sourceKind switch
        {
            TorrentSourceKind.WebPage => provider.GetRequiredService<TorrentWebPageClient>()
                .FindMagnetUriAsync(sourceUri, magnetRegexPattern, cancellationToken),
            TorrentSourceKind.JsonPointer => provider.GetRequiredService<TorrentJsonPointerClient>()
                .FindMagnetUriAsync(sourceUri, magnetRegexPattern, jsonValueFormat, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null),
        };
    }
}
