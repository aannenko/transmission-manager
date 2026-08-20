using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentAddDtoTests
{
    [TestCase("", TestName = "Constructor_WhenSourceUriIsRelative_ThrowsArgumentException(empty string)")]
    [TestCase("/forum/viewtopic.php?t=1")]
    public void Constructor_WhenSourceUriIsRelative_ThrowsArgumentException(string sourceUri)
    {
        Assert.That(
            () => new TorrentAddDto(
                hashString: "0bda511316a069e86dd8ee8a3610475d2013a7fa",
                refreshDate: DateTime.UtcNow,
                name: "TV show name",
                sourceUri: new(sourceUri, UriKind.Relative),
                sourceKind: TorrentSourceKind.WebPage,
                downloadDir: "/tvshows"),
            Throws.ArgumentException);
    }
}
