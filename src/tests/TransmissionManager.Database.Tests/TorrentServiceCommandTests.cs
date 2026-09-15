using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Tests.Helpers;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceCommandTests : BaseTorrentServiceTests
{
    [Test]
    public async Task AddOneAsync_WhenDataDoesNotConflictWithExistingTorrents_AddsTorrentWithVersion1()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New TV show",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234570"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows",
            magnetRegexPattern: @"magnet:\?xt=urn:[^""]+",
            cron: "0 10,18 * * *");

        var (result, torrent) = await service.AddOneAsync(dto).ConfigureAwait(false);

        const long expectedId = 4;

        Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
        TorrentAssertions.AssertEqual(torrent, expectedId, dto);
        Assert.That(torrent!.Version, Is.EqualTo(1));

        var actual = await context.Torrents
            .FirstOrDefaultAsync(static t => t.Id == expectedId)
            .ConfigureAwait(false);

        TorrentAssertions.AssertEqual(actual, expectedId, dto);
        Assert.That(actual!.Version, Is.EqualTo(1));
    }

    [Test]
    public async Task AddOneAsync_WhenSourceKindIsJsonPointer_PersistsItAsStoredIntegerValue()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New TV show",
            sourceUri: new("https://torrentTracker.com/api/1106#/result/6880555/7"),
            sourceKind: TorrentSourceKind.JsonPointer,
            downloadDir: "/tvshows",
            jsonValueFormat: "magnet:?xt=urn:btih:{0}");

        var (result, torrent) = await service.AddOneAsync(dto).ConfigureAwait(false);

        const long expectedId = 4;

        Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
        Assert.That(torrent!.SourceKind, Is.EqualTo(TorrentSourceKind.JsonPointer));

        // Read back through a fresh context so the value comes from SQLite, not the change tracker.
        using var readContext = CreateContext();
        var reloaded = await readContext.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(static t => t.Id == expectedId)
            .ConfigureAwait(false);

        TorrentAssertions.AssertEqual(reloaded, expectedId, dto);

        var storedValue = await readContext.Database
            .SqlQuery<long>($"select SourceKind as Value from Torrents where Id = {expectedId}")
            .SingleAsync()
            .ConfigureAwait(false);

        Assert.That(storedValue, Is.EqualTo(1), "SourceKind must persist as its numeric value.");
    }

    [Test]
    public async Task AddOneAsync_WhenHashStringConflictsWithExistingTorrent_ReturnsNotUnique()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentAddDto(
            hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            refreshDate: DateTime.UtcNow,
            name: "New TV show 2",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234571"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        var (result, torrent) = await service.AddOneAsync(dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotUnique));
            Assert.That(torrent, Is.Null);
        }
    }

    [Test]
    public async Task AddOneAsync_WhenSourceUriConflictsWithExistingTorrent_ReturnsNotUnique()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentAddDto(
            hashString: "96a76b68b91ccf8929c5476e35ce42ff39101d2a",
            refreshDate: DateTime.UtcNow,
            name: "New TV show 3",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234567"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        var (result, torrent) = await service.AddOneAsync(dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotUnique));
            Assert.That(torrent, Is.Null);
        }
    }

    /// <remarks>
    /// Guards the <c>NOCASE</c> collation on the unique indexes: the compiled model carries no
    /// collation annotations, so only <c>OnModelCreating</c> re-applying them keeps these columns
    /// case-insensitive.
    /// </remarks>
    [TestCase(
        "0BDA511316A069E86DD8EE8A3610475D2013A7FA",
        "https://torrentTracker.com/forum/viewtopic.php?t=9999999",
        TestName = "AddOneAsync_WhenAUniqueFieldDiffersOnlyInCase_ReturnsNotUnique(hash string)")]
    [TestCase(
        "96a76b68b91ccf8929c5476e35ce42ff39101d2a",
        "https://torrentTracker.com/FORUM/VIEWTOPIC.PHP?t=1234567",
        TestName = "AddOneAsync_WhenAUniqueFieldDiffersOnlyInCase_ReturnsNotUnique(source URI path)")]
    public async Task AddOneAsync_WhenAUniqueFieldDiffersOnlyInCase_ReturnsNotUnique(
        string hashString,
        string sourceUri)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentAddDto(
            hashString: hashString,
            refreshDate: DateTime.UtcNow,
            name: "New TV show 4",
            sourceUri: new(sourceUri),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        var (result, torrent) = await service.AddOneAsync(dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotUnique));
            Assert.That(torrent, Is.Null);
        }
    }

    /// <remarks>
    /// Pins an accepted trade-off: RFC 6901 member names are case-sensitive, yet uniqueness is
    /// <c>NOCASE</c> over the whole <c>SourceUri</c>, fragment included, so pointers into two
    /// differently-cased members collide.
    /// </remarks>
    [Test]
    public async Task AddOneAsync_WhenSourceUriDiffersOnlyInPointerFragmentCase_ReturnsNotUnique()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var (firstResult, _) = await service.AddOneAsync(new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New JSON show",
            sourceUri: new("https://torrentTracker.com/api/1106#/result/6880555/7"),
            sourceKind: TorrentSourceKind.JsonPointer,
            downloadDir: "/tvshows")).ConfigureAwait(false);

        Assert.That(firstResult, Is.EqualTo(TorrentMutationResult.Success));

        var (secondResult, torrent) = await service.AddOneAsync(new TorrentAddDto(
            hashString: "96a76b68b91ccf8929c5476e35ce42ff39101d2a",
            refreshDate: DateTime.UtcNow,
            name: "New JSON show 2",
            sourceUri: new("https://torrentTracker.com/api/1106#/RESULT/6880555/7"),
            sourceKind: TorrentSourceKind.JsonPointer,
            downloadDir: "/tvshows")).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondResult, Is.EqualTo(TorrentMutationResult.NotUnique));
            Assert.That(torrent, Is.Null);
        }
    }

    /// <remarks>
    /// Guards the OCC contract: were the Id reused, a stale <c>(Id, Version)</c> token would match
    /// a different, newer torrent.
    /// </remarks>
    [Test]
    public async Task AddOneAsync_AfterTheHighestIdWasDeleted_DoesNotReuseThatId()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var deletedId = InitialTorrents[^1].Id;
        var (deleteResult, _) = await service.DeleteOneAsync(deletedId, 1).ConfigureAwait(false);

        Assert.That(deleteResult, Is.EqualTo(TorrentMutationResult.Success));

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New TV show 5",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234571"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        var (addResult, torrent) = await service.AddOneAsync(dto).ConfigureAwait(false);

        Assert.That(addResult, Is.EqualTo(TorrentMutationResult.Success));
        Assert.That(torrent!.Id, Is.GreaterThan(deletedId));
    }

    [Test]
    public async Task AddOneAsync_WhenPreviousAddConflictedOnSameContext_AddsWithoutReplayingFailedInsert()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var conflicting = new TorrentAddDto(
            hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            refreshDate: DateTime.UtcNow,
            name: "Conflicting hash",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234571"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        var (conflictResult, _) = await service.AddOneAsync(conflicting).ConfigureAwait(false);

        var valid = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New TV show",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234572"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        var (validResult, torrent) = await service.AddOneAsync(valid).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(conflictResult, Is.EqualTo(TorrentMutationResult.NotUnique));
            Assert.That(validResult, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(torrent, Is.Not.Null);
        }

        var stored = await context.Torrents.AsNoTracking().ToArrayAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored, Has.Length.EqualTo(4));
            Assert.That(stored.Count(t => t.Name == "Conflicting hash"), Is.Zero);
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenVersionMatches_ReturnsSuccessAndIncrementsVersion()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentUpdateDto(
            hashString: "98ad2e3a694dfc69571c25241bd4042b94a55cf5",
            refreshDate: DateTime.UtcNow,
            name: "New torrent name",
            downloadDir: "/videos",
            magnetRegexPattern: @"magnet:\?xt=[^""]+",
            cron: "1 2,3 4 5 6");

        var (result, currentVersion) = await service.UpdateOneAsync(1, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(currentVersion, Is.EqualTo(2));
        }

        var actual = await context.Torrents.AsNoTracking().FirstOrDefaultAsync(static t => t.Id == 1).ConfigureAwait(false);

        TorrentAssertions.AssertEqual(actual, 1, dto);
        Assert.That(actual!.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateOneAsync_WhenMagnetAndCronAreEmpty_ClearsThemAndIncrementsVersion()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentUpdateDto(magnetRegexPattern: string.Empty, cron: string.Empty);

        var (result, currentVersion) = await service.UpdateOneAsync(3, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(currentVersion, Is.EqualTo(2));
        }

        var actual = await context.Torrents.AsNoTracking().FirstOrDefaultAsync(static t => t.Id == 3).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.MagnetRegexPattern, Is.Null);
            Assert.That(actual.Cron, Is.Null);
            Assert.That(actual.Version, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenJsonValueFormatIsSetThenEmptied_PersistsThenClearsIt()
    {
        const string format = "magnet:?xt=urn:btih:{0}";
        using var context = CreateContext();
        var service = CreateService(context);

        var (setResult, versionAfterSet) = await service
            .UpdateOneAsync(3, 1, new(jsonValueFormat: format))
            .ConfigureAwait(false);

        var afterSet = await context.Torrents.AsNoTracking()
            .FirstAsync(static t => t.Id == 3)
            .ConfigureAwait(false);

        var (clearResult, _) = await service
            .UpdateOneAsync(3, versionAfterSet!.Value, new(jsonValueFormat: string.Empty))
            .ConfigureAwait(false);

        var afterClear = await context.Torrents.AsNoTracking()
            .FirstAsync(static t => t.Id == 3)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(setResult, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(afterSet.JsonValueFormat, Is.EqualTo(format));
            Assert.That(clearResult, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(afterClear.JsonValueFormat, Is.Null);
        }
    }

    /// <remarks>
    /// A torrent's source is immutable through this path - re-pointing is done by adding the new
    /// address as its own torrent and deleting the old record. Asserted rather than assumed, because
    /// nothing else stops a <c>SetProperty</c> for either column being added back.
    /// </remarks>
    [Test]
    public async Task UpdateOneAsync_WhenEveryOtherFieldChanges_LeavesTheSourceUntouched()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentUpdateDto(
            hashString: "ffffffffffffffffffffffffffffffffffffffff",
            refreshDate: DateTime.UtcNow,
            name: "New torrent name",
            downloadDir: "/movies",
            magnetRegexPattern: @"magnet:\?xt=urn:btih:[^""]+",
            jsonValueFormat: "magnet:?xt=urn:btih:{0}",
            cron: "0 9,17 * * *");

        var (result, _) = await service.UpdateOneAsync(1, 1, dto).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));

        var actual = await context.Torrents.AsNoTracking().FirstOrDefaultAsync(static t => t.Id == 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.SourceUri, Is.EqualTo(InitialTorrents[0].SourceUri));
            Assert.That(actual.SourceKind, Is.EqualTo(InitialTorrents[0].SourceKind));
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var dto = new TorrentUpdateDto(name: "irrelevant");
        var (result, currentVersion) = await service.UpdateOneAsync(-1, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(currentVersion, Is.Null);
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenVersionMismatches_ReturnsConflictWithCurrentVersionAndRowUnchanged()
    {
        const int torrentId = 2;
        using var context = CreateContext();
        var service = CreateService(context);

        var originalTorrent = await context.Torrents.AsNoTracking()
            .FirstAsync(static t => t.Id == torrentId)
            .ConfigureAwait(false);

        var dto = new TorrentUpdateDto(name: "Mutation that should not happen");
        var (result, currentVersion) = await service.UpdateOneAsync(2, 999, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.VersionConflict));
            Assert.That(currentVersion, Is.EqualTo(1));
        }

        var actual = await context.Torrents.AsNoTracking().FirstOrDefaultAsync(static t => t.Id == 2).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Name, Is.EqualTo(originalTorrent.Name));
            Assert.That(actual.Version, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UpdateOneAsync_TwiceWithSameCapturedVersion_SecondReturnsConflict()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        var cache = CreateCache();
        var service1 = CreateService(context1, cache);
        var service2 = CreateService(context2, cache);

        var dto1 = new TorrentUpdateDto(name: "first writer");
        var dto2 = new TorrentUpdateDto(name: "second writer (loses)");

        var (result1, version1) = await service1.UpdateOneAsync(2, 1, dto1).ConfigureAwait(false);
        var (result2, version2) = await service2.UpdateOneAsync(2, 1, dto2).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(version1, Is.EqualTo(2));
            Assert.That(result2, Is.EqualTo(TorrentMutationResult.VersionConflict));
            Assert.That(version2, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task DeleteOneAsync_WhenVersionMatches_ReturnsSuccess()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var (result, currentVersion) = await service.DeleteOneAsync(2, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(currentVersion, Is.EqualTo(1));
        }

        var actual = await context.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(static t => t.Id == 2)
            .ConfigureAwait(false);

        Assert.That(actual, Is.Null);
    }

    [Test]
    public async Task DeleteOneAsync_WhenVersionMismatches_ReturnsConflictWithCurrentVersionAndRowExists()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var (result, currentVersion) = await service.DeleteOneAsync(3, 999).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.VersionConflict));
            Assert.That(currentVersion, Is.EqualTo(1));
        }

        var actual = await context.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(static t => t.Id == 3)
            .ConfigureAwait(false);

        Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task DeleteOneAsync_TwiceWithSameCapturedVersion_SecondReturnsNotFound()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        var cache = CreateCache();
        var service1 = CreateService(context1, cache);
        var service2 = CreateService(context2, cache);

        var (result1, version1) = await service1.DeleteOneAsync(2, 1).ConfigureAwait(false);
        var (result2, version2) = await service2.DeleteOneAsync(2, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(version1, Is.EqualTo(1));
            Assert.That(result2, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(version2, Is.Null);
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenRowDeletedConcurrently_ReturnsNotFoundNotConflict()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        var cache = CreateCache();
        var service1 = CreateService(context1, cache);
        var service2 = CreateService(context2, cache);

        // service1's update will fail (predicate cannot match) because service2 deletes the row first.
        // The disambiguating SELECT then sees no row and returns NotFound (rather than Conflict).
        var (deleteResult, _) = await service2.DeleteOneAsync(2, 1).ConfigureAwait(false);
        Assume.That(deleteResult, Is.EqualTo(TorrentMutationResult.Success));

        var dto = new TorrentUpdateDto(name: "writer that finds the row gone");
        var (result, currentVersion) = await service1.UpdateOneAsync(2, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(currentVersion, Is.Null);
        }
    }

    [Test]
    public async Task DeleteOneAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var (result, currentVersion) = await service.DeleteOneAsync(-1, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(currentVersion, Is.Null);
        }
    }

    [Test]
    public async Task AddOneAsync_WhenSuccessful_InvalidatesCachedCount()
    {
        var cache = CreateCache();
        using var context = CreateContext();
        var service = CreateService(context, cache);

        var before = await service.GetCountAsync().ConfigureAwait(false);

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New TV show",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234570"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        _ = await service.AddOneAsync(dto).ConfigureAwait(false);
        var after = await service.GetCountAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(before, Is.EqualTo(3));
            Assert.That(after, Is.EqualTo(4));
        }
    }

    [Test]
    public async Task AddOneAsync_WhenConflicts_DoesNotInvalidateCachedCount()
    {
        var cache = CreateCache();
        using var context = CreateContext();
        var service = CreateService(context, cache);

        var before = await service.GetCountAsync().ConfigureAwait(false);

        var dto = new TorrentAddDto(
            hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            refreshDate: DateTime.UtcNow,
            name: "Conflicting hash",
            sourceUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234571"),
            sourceKind: TorrentSourceKind.WebPage,
            downloadDir: "/tvshows");

        Assert.That(
            (await service.AddOneAsync(dto).ConfigureAwait(false)).Result,
            Is.EqualTo(TorrentMutationResult.NotUnique));

        var after = await service.GetCountAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(before, Is.EqualTo(3));
            Assert.That(after, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenHashStringConflictsWithExistingTorrent_ReturnsNotUnique()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        // Torrent 2's hash; repointing torrent 1 at it violates the unique index on HashString.
        // ExecuteUpdateAsync bypasses the change tracker, so SQLite throws a bare SqliteException
        // rather than a DbUpdateException; before this was mapped it escaped as an HTTP 500.
        var dto = new TorrentUpdateDto(hashString: "738c60cbd44f0e9457ba2afdad9e9231d76243fe");

        var (result, currentVersion) = await service.UpdateOneAsync(1, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotUnique));
            Assert.That(currentVersion, Is.Null);
        }

        var unchanged = await context.Torrents.AsNoTracking()
            .FirstAsync(static t => t.Id == 1)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unchanged.HashString, Is.EqualTo("0bda511316a069e86dd8ee8a3610475d2013a7fa"));
            Assert.That(unchanged.Version, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenTargetHashIsUnchanged_ReturnsSuccess()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        // Re-writing a row's own hash must not trip the unique index.
        var dto = new TorrentUpdateDto(hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa");

        var (result, currentVersion) = await service.UpdateOneAsync(1, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(currentVersion, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task DeleteOneAsync_WhenSuccessful_InvalidatesCachedCount()
    {
        var cache = CreateCache();
        using var context = CreateContext();
        var service = CreateService(context, cache);

        var before = await service.GetCountAsync().ConfigureAwait(false);

        var (result, _) = await service.DeleteOneAsync(2, 1).ConfigureAwait(false);
        var after = await service.GetCountAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(before, Is.EqualTo(3));
            Assert.That(after, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task DeleteOneAsync_WhenNotFoundOrConflict_DoesNotInvalidateCachedCount()
    {
        var cache = CreateCache();
        using var context = CreateContext();
        var service = CreateService(context, cache);

        var before = await service.GetCountAsync().ConfigureAwait(false);

        var (notFound, _) = await service.DeleteOneAsync(-1, 1).ConfigureAwait(false);
        var (conflict, _) = await service.DeleteOneAsync(2, 999).ConfigureAwait(false);
        var after = await service.GetCountAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notFound, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(conflict, Is.EqualTo(TorrentMutationResult.VersionConflict));
            Assert.That(before, Is.EqualTo(3));
            Assert.That(after, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenSuccessful_InvalidatesCachedFilteredCount()
    {
        var cache = CreateCache();
        using var context = CreateContext();
        var service = CreateService(context, cache);

        var filter = new TorrentFilter(PropertyStartsWith: "renamed");
        var before = await service.GetCountAsync(filter).ConfigureAwait(false);

        var (result, _) = await service
            .UpdateOneAsync(2, 1, new TorrentUpdateDto(name: "renamed movie"))
            .ConfigureAwait(false);

        var after = await service.GetCountAsync(filter).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(before, Is.Zero);
            Assert.That(after, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UpdateOneAsync_WhenConflict_DoesNotInvalidateCachedCount()
    {
        var cache = CreateCache();
        using var context = CreateContext();
        var service = CreateService(context, cache);

        var filter = new TorrentFilter(PropertyStartsWith: "renamed");
        var before = await service.GetCountAsync(filter).ConfigureAwait(false);

        var (result, _) = await service
            .UpdateOneAsync(2, 999, new TorrentUpdateDto(name: "renamed movie"))
            .ConfigureAwait(false);

        var after = await service.GetCountAsync(filter).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.VersionConflict));
            Assert.That(before, Is.Zero);
            Assert.That(after, Is.Zero);
        }
    }
}
