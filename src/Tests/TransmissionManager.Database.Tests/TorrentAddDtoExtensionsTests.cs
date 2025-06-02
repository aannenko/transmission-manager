using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Tests.Helpers;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentAddDtoExtensionsTests
{
    [Test]
    public void ToTorrent_WhenDtoIsValid_ReturnsTorrentWithPropertiesCorrectlyMapped()
    {
        var dto = new TorrentAddDto(
            hashString: "ABCDEF0123456789ABCDEF0123456789ABCDEF01",
            refreshDate: DateTime.UtcNow,
            name: "Test name",
            webPageUri: new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            downloadDir: "/tvshows",
            magnetRegexPattern: @"magnet:\?xt=[^""]+",
            cron: "0 9,17 * * *");

        var torrent = dto.ToTorrent();

        TorrentAssertions.AssertEqual(torrent, 0, dto);
    }
}
