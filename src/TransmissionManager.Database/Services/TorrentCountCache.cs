using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Services;

/// <remarks>
/// Caches filtered torrent counts in a private <see cref="MemoryCache"/> keyed by the (normalized)
/// <see cref="TorrentFilter"/>. Every cached entry is linked to the current <see cref="Generation"/>'s
/// change token; a successful mutation calls <see cref="Invalidate"/>, which atomically swaps in a
/// fresh generation and cancels the old one's token (evicting all entries linked to it). Snapshotting
/// the generation's token before the recompute factory runs also resolves the stale-fill race: a slow
/// factory that started before an invalidation writes under the now-cancelled token, so its entry is
/// dropped on insert and a stale count is never served (a later lookup recomputes against the fresh
/// token). Entries carry no time-based expiration: a count stays valid until the next mutation, and
/// every mutation invalidates the cache.
/// </remarks>
public sealed class TorrentCountCache : IDisposable
{
    private sealed record CacheKey(TorrentFilter Filter);

    private sealed record Generation(CancellationTokenSource Source, CancellationChangeToken Token);

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private Generation _generation = NewGeneration();

    public async ValueTask<long> GetOrAddAsync<TArg>(
        TorrentFilter filter,
        Func<TArg, CancellationToken, Task<long>> factory,
        TArg factoryArgument,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var key = new CacheKey(Normalize(filter));
        if (_cache.TryGetValue(key, out long count))
            return count;

        var token = Volatile.Read(ref _generation).Token;
        count = await factory(factoryArgument, cancellationToken).ConfigureAwait(false);
        _ = _cache.Set(key, count, token);
        return count;
    }

    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _generation, NewGeneration());
        old.Source.Cancel();
        old.Source.Dispose();
    }

    public void Dispose()
    {
        // Safe only because this is a DI singleton: Dispose runs solely at root-container teardown
        // (process shutdown). A fire-and-forget mutation (e.g. BackgroundTorrentUpdateService) could
        // still race Invalidate here, but the window is process-exit only and CancellationTokenSource
        // .Dispose is idempotent, so the worst case is a swallowed ObjectDisposedException from Cancel
        // as the process terminates. If this cache ever becomes non-singleton (disposed during normal
        // operation while mutations run), add a guard: mutual exclusion between Invalidate and
        // Dispose (and the cache Set path), keeping the GetOrAddAsync TryGetValue fast path lock-free.
        _generation.Source.Dispose();
        _cache.Dispose();
    }

    private static Generation NewGeneration()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope - owned by _generation, disposed in Invalidate/Dispose
        var source = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
        return new Generation(source, new CancellationChangeToken(source.Token));
    }

    private static TorrentFilter Normalize(in TorrentFilter filter)
    {
        return string.IsNullOrEmpty(filter.PropertyStartsWith)
            ? filter with { PropertyStartsWith = null }
            : filter;
    }
}
