using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Helpers;

internal static class TorrentAssertions
{
    public static void AssertEqual(
        TorrentDto? actual,
        long expectedId,
        Torrent expected,
        TimeSpan refreshDateTolerance = default)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(expectedId));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));

            if (refreshDateTolerance == default)
            {
                Assert.That(actual.RefreshDate, Is.EqualTo(expected.RefreshDate));
            }
            else
            {
                var toleranceTimeSpan = refreshDateTolerance;
                Assert.That(actual.RefreshDate, Is.EqualTo(expected.RefreshDate).Within(toleranceTimeSpan));
            }

            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri.OriginalString, Is.EqualTo(expected.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
        }
    }
}
