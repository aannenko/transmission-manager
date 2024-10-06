using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
public sealed class TorrentCommandServiceTests : BaseTorrentServiceTests
{
    [Test]
    public async Task AddOneAsync_AddsTorrent_WhenItDoesNotConflictWithExistingTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var dto = new TorrentAddDto(
            hashString: "33de7f6754ec58653f0ff349d70578c144268a8e",
            name: "New TV show",
            webPageUri: "https://torrentTracker.com/forum/viewtopic.php?t=1234570",
            downloadDir: "/tvshows",
            magnetRegexPattern: @"\""(?<magnet>magnet:\?xt=urn:.*?)\""",
            cron: "0 10,18 * * *");

        var torrentId = await service.AddOneAsync(dto);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == torrentId);

        Assert.Multiple(() =>
        {
            Assert.That(torrentId, Is.GreaterThan(0));
            Assert.That(actual, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(torrentId));
            Assert.That(actual.HashString, Is.EqualTo(dto.HashString));
            Assert.That(actual.Name, Is.EqualTo(dto.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(dto.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(dto.Cron));
        });
    }

    [Test]
    public void AddOneAsync_ThrowsDbUpdateException_WhenHashStringConflictsWithExistingTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var dto = new TorrentAddDto(
            hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            name: "New TV show 2",
            webPageUri: "https://torrentTracker.com/forum/viewtopic.php?t=1234571",
            downloadDir: "/tvshows");

        Assert.That(async () => await service.AddOneAsync(dto), Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public void AddOneAsync_ThrowsDbUpdateException_WhenWebPageConflictsWithExistingTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var dto = new TorrentAddDto(
            hashString: "96a76b68b91ccf8929c5476e35ce42ff39101d2a",
            name: "New TV show 3",
            webPageUri: "https://torrentTracker.com/forum/viewtopic.php?t=1234567",
            downloadDir: "/tvshows");

        Assert.That(async () => await service.AddOneAsync(dto), Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_UpdatesTorrent_WhenItCanBeFoundById()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var dto = new TorrentUpdateDto(
            hashString: "98ad2e3a694dfc69571c25241bd4042b94a55cf5",
            name: "New torrent name",
            downloadDir: "/videos",
            magnetRegexPattern: @"\""(?<magnet>magnet:\?xt=.*?)\""",
            cron: "1 2,3 4 5 6");

        var isUpdated = await service.TryUpdateOneByIdAsync(1, dto);

        Assert.That(isUpdated);

        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 1);

        Assert.That(actual, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(actual.HashString, Is.EqualTo(dto.HashString));
            Assert.That(actual.Name, Is.EqualTo(dto.Name));
            Assert.That(actual.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(dto.Cron));
        });
    }

    [Test]
    public async Task TryUpdateOneByIdAsync_DoesNotUpdateTorrent_WhenItCannotBeFoundById()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var dto = new TorrentUpdateDto(hashString: "98ad2e3a694dfc69571c25241bd4042b94a55cf5");

        var isUpdated = await service.TryUpdateOneByIdAsync(-1, dto);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == -1);

        Assert.Multiple(() =>
        {
            Assert.That(!isUpdated);
            Assert.That(actual, Is.Null);
        });
    }

    [Test]
    public async Task TryDeleteOneByIdAsync_DeletesTorrent_WhenItCanBeFoundById()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var isDeleted = await service.TryDeleteOneByIdAsync(2);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == 2);

        Assert.Multiple(() =>
        {
            Assert.That(isDeleted);
            Assert.That(actual, Is.Null);
        });
    }

    [Test]
    public async Task TryDeleteOneByIdAsync_DoesNotDeleteTorrent_WhenItCannotBeFoundById()
    {
        using var context = CreateContext();
        var service = new TorrentCommandService(context);

        var isDeleted = await service.TryDeleteOneByIdAsync(-1);
        var actual = await context.Torrents.FirstOrDefaultAsync(torrent => torrent.Id == -1);

        Assert.Multiple(() =>
        {
            Assert.That(!isDeleted);
            Assert.That(actual, Is.Null);
        });
    }
}