using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceCommandTests : BaseTorrentServiceTests
{
    [Test]
    public async Task AddOneAsync_WhenGivenDataNotConflictingWithExistingTorrents_AddsTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            name: "New TV show",
            webPageUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234570"),
            downloadDir: "/tvshows",
            magnetRegexPattern: @"magnet:\?xt=urn:[^""]+",
            cron: "0 10,18 * * *");

        var torrentId = await service.AddOneAsync(dto).ConfigureAwait(false);
        var actual = await context.Torrents
            .FirstOrDefaultAsync(torrent => torrent.Id == torrentId)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(torrentId, Is.GreaterThan(0));
            Assert.That(actual, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(torrentId));
            Assert.That(actual.HashString, Is.EqualTo(dto.HashString));
            Assert.That(actual.Name, Is.EqualTo(dto.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(dto.WebPageUri.OriginalString));
            Assert.That(actual.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(dto.Cron));
        }
    }

    [Test]
    public void AddOneAsync_WhenGivenHashStringConflictingWithExistingTorrent_ThrowsDbUpdateException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentAddDto(
            hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            name: "New TV show 2",
            webPageUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234571"),
            downloadDir: "/tvshows");

        var task = service.AddOneAsync(dto).ConfigureAwait(false);

        Assert.That(async () => await task, Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public void AddOneAsync_WhenGivenWebPageConflictingWithExistingTorrent_ThrowsDbUpdateException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentAddDto(
            hashString: "96a76b68b91ccf8929c5476e35ce42ff39101d2a",
            name: "New TV show 3",
            webPageUri: new("https://torrentTracker.com/forum/viewtopic.php?t=1234567"),
            downloadDir: "/tvshows");

        var task = service.AddOneAsync(dto).ConfigureAwait(false);

        Assert.That(async () => await task, Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_WhenGivenExistingTorrentId_UpdatesTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentUpdateDto(
            hashString: "98ad2e3a694dfc69571c25241bd4042b94a55cf5",
            name: "New torrent name",
            downloadDir: "/videos",
            magnetRegexPattern: @"magnet:\?xt=[^""]+",
            cron: "1 2,3 4 5 6");

        var isUpdated = await service.TryUpdateOneByIdAsync(1, dto).ConfigureAwait(false);

        Assert.That(isUpdated);

        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 1).ConfigureAwait(false);

        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.HashString, Is.EqualTo(dto.HashString));
            Assert.That(actual.Name, Is.EqualTo(dto.Name));
            Assert.That(actual.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(dto.Cron));
        }
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_WhenGivenEmptyStringMagnetAndCron_SetsMagnetAndCronToNull()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var dto = new TorrentUpdateDto(magnetRegexPattern: string.Empty, cron: string.Empty);

        var isUpdated = await service.TryUpdateOneByIdAsync(1, dto).ConfigureAwait(false);

        Assert.That(isUpdated);

        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 1).ConfigureAwait(false);

        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.MagnetRegexPattern, Is.Null);
            Assert.That(actual.Cron, Is.Null);
        }
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_WhenGivenNonExistentTorrentId_DoesNotUpdateTorrent()
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
    public async Task TryDeleteOneByIdAsync_WhenGivenExistingTorrentId_DeletesTorrent()
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
    public async Task TryDeleteOneByIdAsync_WhenGivenNonExistentTorrentId_DoesNotDeleteTorrent()
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
