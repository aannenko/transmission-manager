using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Helpers;

internal static class TorrentAssertions
{
    public static void AssertEqual(Torrent actual, long expectedId, Torrent expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(expectedId));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(expected.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
        });
    }
}
