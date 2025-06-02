using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using TransmissionManager.Database.Tests.Helpers;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceCommandTests : BaseTorrentServiceTests
{
    [Test]
    public async Task AddOneAsync_WhenDataDoesNotConflictWithExistingTorrents_AddsTorrent()
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

        var torrentId = await service.AddOneAsync(dto).ConfigureAwait(false);
        
        Assert.That(torrentId, Is.GreaterThan(0));
        
        var actual = await context.Torrents
            .FirstOrDefaultAsync(torrent => torrent.Id == torrentId)
            .ConfigureAwait(false);

        TorrentAssertions.AssertEqual(actual, torrentId, dto);
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

        var task = service.AddOneAsync(dto).ConfigureAwait(false);

        Assert.That(async () => await task, Throws.TypeOf<DbUpdateException>());
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

        var task = service.AddOneAsync(dto).ConfigureAwait(false);

        Assert.That(async () => await task, Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_WhenIdExistsAndDataIsValid_UpdatesTorrent()
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

        var isUpdated = await service.TryUpdateOneByIdAsync(1, dto).ConfigureAwait(false);

        Assert.That(isUpdated);

        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 1).ConfigureAwait(false);

        TorrentAssertions.AssertEqual(actual, 1, dto);
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_WhenMagnetAndCronAreEmptyStrings_SetsMagnetAndCronToNull()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentUpdateDto(magnetRegexPattern: string.Empty, cron: string.Empty);

        var isUpdated = await service.TryUpdateOneByIdAsync(1, dto).ConfigureAwait(false);

        Assert.That(isUpdated);

        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 1).ConfigureAwait(false);

        TorrentAssertions.AssertEqual(actual, 1, dto);
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_WhenIdDoesNotExist_DoesNotUpdateTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentUpdateDto(hashString: "98ad2e3a694dfc69571c25241bd4042b94a55cf5");

        var isUpdated = await service.TryUpdateOneByIdAsync(-1, dto).ConfigureAwait(false);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == -1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(!isUpdated);
            Assert.That(actual, Is.Null);
        }
    }

    [Test]
    public async Task TryDeleteOneByIdAsync_WhenIdExists_DeletesTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var isDeleted = await service.TryDeleteOneByIdAsync(2).ConfigureAwait(false);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 2).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isDeleted);
            Assert.That(actual, Is.Null);
        }
    }

    [Test]
    public async Task TryDeleteOneByIdAsync_WhenIdDoesNotExist_DoesNotDeleteTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var isDeleted = await service.TryDeleteOneByIdAsync(-1).ConfigureAwait(false);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == -1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(!isDeleted);
            Assert.That(actual, Is.Null);
        }
    }
}
