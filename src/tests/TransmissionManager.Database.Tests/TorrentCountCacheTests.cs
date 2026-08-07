using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentCountCacheTests
{
    private TorrentCountCache _cache = null!;

    [SetUp]
    public void SetUp() => _cache = new TorrentCountCache();

    [TearDown]
    public void TearDown() => _cache.Dispose();

    [Test]
    public async Task GetOrAddAsync_WhenKeyIsCold_InvokesFactoryOnceAndReturnsValue()
    {
        var calls = 0;

        var result = await _cache.GetOrAddAsync(default, (_, _) =>
        {
            calls++;
            return Task.FromResult(42L);
        }, 0).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(42));
            Assert.That(calls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetOrAddAsync_WhenKeyIsWarm_DoesNotInvokeFactoryAgain()
    {
        var calls = 0;
        Func<int, CancellationToken, Task<long>> factory = (_, _) =>
        {
            calls++;
            return Task.FromResult((long)calls);
        };

        var first = await _cache.GetOrAddAsync(default, factory, 0).ConfigureAwait(false);
        var second = await _cache.GetOrAddAsync(default, factory, 0).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetOrAddAsync_AfterInvalidate_RecomputesValue()
    {
        var calls = 0;
        Func<int, CancellationToken, Task<long>> factory = (_, _) => Task.FromResult((long)++calls);

        var first = await _cache.GetOrAddAsync(default, factory, 0).ConfigureAwait(false);
        _cache.Invalidate();
        var second = await _cache.GetOrAddAsync(default, factory, 0).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(2));
            Assert.That(calls, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task GetOrAddAsync_DistinctFilters_AreCachedSeparately()
    {
        var calls = 0;
        Func<int, CancellationToken, Task<long>> factory = (_, _) => Task.FromResult((long)++calls);

        var a = await _cache.GetOrAddAsync(new(PropertyStartsWith: "a"), factory, 0).ConfigureAwait(false);
        var b = await _cache.GetOrAddAsync(new(CronExists: true), factory, 0).ConfigureAwait(false);
        var aAgain = await _cache.GetOrAddAsync(new(PropertyStartsWith: "a"), factory, 0).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(a, Is.EqualTo(1));
            Assert.That(b, Is.EqualTo(2));
            Assert.That(aAgain, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task GetOrAddAsync_NormalizesEmptyPropertyStartsWith_ToUnfilteredKey()
    {
        var calls = 0;
        Func<int, CancellationToken, Task<long>> factory = (_, _) => Task.FromResult((long)++calls);

        var unfiltered = await _cache.GetOrAddAsync(default, factory, 0).ConfigureAwait(false);
        var empty = await _cache
            .GetOrAddAsync(new(PropertyStartsWith: string.Empty), factory, 0)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unfiltered, Is.EqualTo(1));
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetOrAddAsync_WhitespacePropertyStartsWith_IsADistinctKey()
    {
        var calls = 0;
        Func<int, CancellationToken, Task<long>> factory = (_, _) => Task.FromResult((long)++calls);

        var unfiltered = await _cache.GetOrAddAsync(default, factory, 0).ConfigureAwait(false);
        var whitespace = await _cache
            .GetOrAddAsync(new(PropertyStartsWith: " "), factory, 0)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unfiltered, Is.EqualTo(1));
            Assert.That(whitespace, Is.EqualTo(2));
            Assert.That(calls, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task GetOrAddAsync_WhenInvalidatedDuringCompute_NeverServesTheStaleValue()
    {
        var factoryStarted = new TaskCompletionSource();
        var release = new TaskCompletionSource();

        // First lookup snapshots the current invalidation token, then blocks inside the factory.
        var slowLookup = _cache.GetOrAddAsync(default, async (_, _) =>
        {
            factoryStarted.SetResult();
            await release.Task.ConfigureAwait(false);
            return 1L;
        }, 0).AsTask();

        await factoryStarted.Task.ConfigureAwait(false);

        // A concurrent mutation invalidates the cache while the slow factory is still running.
        _cache.Invalidate();

        // Let the slow factory complete; it writes 1 under the now-cancelled token, so the entry
        // is dropped on insert.
        release.SetResult();
        var stale = await slowLookup.ConfigureAwait(false);

        // A subsequent read misses (entry was evicted) and recomputes; the stale value is never served.
        var fresh = await _cache.GetOrAddAsync(default, (_, _) => Task.FromResult(2L), 0).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stale, Is.EqualTo(1));
            Assert.That(fresh, Is.EqualTo(2));
        }
    }
}
