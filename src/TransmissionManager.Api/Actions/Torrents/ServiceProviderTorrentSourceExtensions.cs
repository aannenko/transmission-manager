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
    /// The pattern that extracts the magnet from a web page, or <see langword="null"/> to use the
    /// configured default. Not passed on for any other kind - see the remarks.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="MagnetSearchOutcome"/> representing the result of the search, including failures.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided <paramref name="sourceKind"/> is not a defined member.
    /// </exception>
    /// <remarks>
    /// A torrent keeps its <c>MagnetRegexPattern</c> across a change of kind, so a JSON Pointer
    /// source can carry one. It is not passed on rather than being rejected, because
    /// <see cref="TorrentJsonPointerClient"/> has no parameter to receive it.
    /// </remarks>
    public static Task<MagnetSearchOutcome> FindMagnetUriAsync(
        this IServiceProvider provider,
        Uri sourceUri,
        TorrentSourceKind sourceKind,
        string? magnetRegexPattern,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return sourceKind switch
        {
            TorrentSourceKind.WebPage => provider.GetRequiredService<TorrentWebPageClient>()
                .FindMagnetUriAsync(sourceUri, magnetRegexPattern, cancellationToken),
            TorrentSourceKind.JsonPointer => provider.GetRequiredService<TorrentJsonPointerClient>()
                .FindMagnetUriAsync(sourceUri, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null),
        };
    }
}
