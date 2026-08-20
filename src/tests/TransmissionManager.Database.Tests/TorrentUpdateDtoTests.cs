using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentUpdateDtoTests
{
    [Test]
    public void Constructor_WhenAllFieldsAreNull_ThrowsArgumentException()
    {
        Assert.That(static () => new TorrentUpdateDto(), Throws.ArgumentException);
    }

    [Test]
    public void Constructor_WhenOnlyDownloadDirIsProvided_Succeeds()
    {
        var dto = new TorrentUpdateDto(downloadDir: "/tvshows");

        Assert.That(dto.DownloadDir, Is.EqualTo("/tvshows"));
    }

    [TestCase("", TestName = "Constructor_WhenDownloadDirIsBlank_ThrowsArgumentException(empty string)")]
    [TestCase("   ", TestName = "Constructor_WhenDownloadDirIsBlank_ThrowsArgumentException(whitespace)")]
    public void Constructor_WhenDownloadDirIsBlank_ThrowsArgumentException(string downloadDir)
    {
        Assert.That(() => new TorrentUpdateDto(downloadDir: downloadDir), Throws.ArgumentException);
    }
}
