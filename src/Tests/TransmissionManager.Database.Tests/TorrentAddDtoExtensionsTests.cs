using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Extensions;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentAddDtoExtensionsTests
{
    [Test]
    public void ToTorrent_WhenGivenValidDto_ReturnsTorrentWithPropertiesCorrectlyMapped()
    {
        var dto = new TorrentAddDto(
            hashString: "ABCDEF0123456789ABCDEF0123456789ABCDEF01",
            name: "Test name",
            webPageUri: new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            downloadDir: "/tvshows",
            magnetRegexPattern: @"magnet:\?xt=[^""]*",
            cron: "0 9,17 * * *");

        var torrent = dto.ToTorrent();

        Assert.That(torrent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(torrent.Id, Is.Zero);
            Assert.That(torrent.HashString, Is.EqualTo(dto.HashString));
            Assert.That(torrent.Name, Is.EqualTo(dto.Name));
            Assert.That(torrent.WebPageUri, Is.EqualTo(dto.WebPageUri.OriginalString));
            Assert.That(torrent.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(torrent.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(torrent.Cron, Is.EqualTo(dto.Cron));
        });
    }

    [Test]
    public void ToTorrent_WhenGivenNull_ThrowsArgumentNullException()
    {
        TorrentAddDto dto = null!;
        Assert.That(dto.ToTorrent, Throws.ArgumentNullException);
    }
}
