using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using TransmissionManager.Database.Tests.Helpers;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceCommandTests : BaseTorrentServiceTests
{
    [Test]
    public async Task AddOneAsync_WhenDataDoesNotConflictWithExistingTorrents_AddsTorrentWithVersion1()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            refreshDate: DateTime.UtcNow,
            name: "New TV show",
            webPageUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234570"),
            downloadDir: "/tvshows",
            magnetRegexPattern: @"magnet:\?xt=urn:[^""]+",
            cron: "0 10,18 * * *");

        var torrent = await service.AddOneAsync(dto).ConfigureAwait(false);

        const long expectedId = 4;

        TorrentAssertions.AssertEqual(torrent, expectedId, dto);
        Assert.That(torrent.Version, Is.EqualTo(1));

        var actual = await context.Torrents
            .FirstOrDefaultAsync(static t => t.Id == expectedId)
            .ConfigureAwait(false);

        TorrentAssertions.AssertEqual(actual, expectedId, dto);
        Assert.That(actual!.Version, Is.EqualTo(1));
    }

    [Test]
    public void AddOneAsync_WhenHashStringConflictsWithExistingTorrent_ThrowsDbUpdateException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentAddDto(
            hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            refreshDate: DateTime.UtcNow,
            name: "New TV show 2",
            webPageUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234571"),
            downloadDir: "/tvshows");

        Assert.That(
            async () => await service.AddOneAsync(dto).ConfigureAwait(false),
            Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public void AddOneAsync_WhenWebPageUriConflictsWithExistingTorrent_ThrowsDbUpdateException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentAddDto(
            hashString: "96a76b68b91ccf8929c5476e35ce42ff39101d2a",
            refreshDate: DateTime.UtcNow,
            name: "New TV show 3",
            webPageUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234567"),
            downloadDir: "/tvshows");

        Assert.That(
            async () => await service.AddOneAsync(dto).ConfigureAwait(false),
            Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public async Task TryUpdateOneAsync_WhenVersionMatches_ReturnsSuccessAndIncrementsVersion()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

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
    public async Task TryUpdateOneAsync_WhenMagnetAndCronAreEmpty_ClearsThemAndIncrementsVersion()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

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
    public async Task TryUpdateOneAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentUpdateDto(name: "irrelevant");
        var (result, currentVersion) = await service.UpdateOneAsync(-1, 1, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(currentVersion, Is.Null);
        }
    }

    [Test]
    public async Task TryUpdateOneAsync_WhenVersionMismatches_ReturnsConflictWithCurrentVersionAndRowUnchanged()
    {
        const int torrentId = 2;
        using var context = CreateContext();
        var service = new TorrentService(context);

        var originalTorrent = await context.Torrents.AsNoTracking()
            .FirstAsync(static t => t.Id == torrentId)
            .ConfigureAwait(false);

        var dto = new TorrentUpdateDto(name: "Mutation that should not happen");
        var (result, currentVersion) = await service.UpdateOneAsync(2, 999, dto).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Conflict));
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
    public async Task TryUpdateOneAsync_TwiceWithSameCapturedVersion_SecondReturnsConflict()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        var service1 = new TorrentService(context1);
        var service2 = new TorrentService(context2);

        var dto1 = new TorrentUpdateDto(name: "first writer");
        var dto2 = new TorrentUpdateDto(name: "second writer (loses)");

        var (result1, version1) = await service1.UpdateOneAsync(2, 1, dto1).ConfigureAwait(false);
        var (result2, version2) = await service2.UpdateOneAsync(2, 1, dto2).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.EqualTo(TorrentMutationResult.Success));
            Assert.That(version1, Is.EqualTo(2));
            Assert.That(result2, Is.EqualTo(TorrentMutationResult.Conflict));
            Assert.That(version2, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task TryDeleteOneAsync_WhenVersionMatches_ReturnsSuccess()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

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
    public async Task TryDeleteOneAsync_WhenVersionMismatches_ReturnsConflictWithCurrentVersionAndRowExists()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var (result, currentVersion) = await service.DeleteOneAsync(3, 999).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.Conflict));
            Assert.That(currentVersion, Is.EqualTo(1));
        }

        var actual = await context.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(static t => t.Id == 3)
            .ConfigureAwait(false);

        Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task TryDeleteOneAsync_TwiceWithSameCapturedVersion_SecondReturnsNotFound()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        var service1 = new TorrentService(context1);
        var service2 = new TorrentService(context2);

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
    public async Task TryUpdateOneAsync_WhenRowDeletedConcurrently_ReturnsNotFoundNotConflict()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        var service1 = new TorrentService(context1);
        var service2 = new TorrentService(context2);

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
    public async Task TryDeleteOneAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var (result, currentVersion) = await service.DeleteOneAsync(-1, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TorrentMutationResult.NotFound));
            Assert.That(currentVersion, Is.Null);
        }
    }
}
