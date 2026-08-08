using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Helpers;

internal static class TorrentAssertions
{
    public static void AssertEqual(
        TorrentDto? actual,
        Torrent expected,
        TimeSpan refreshDateTolerance = default)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));

            var expectedDateTimeOffset = new DateTimeOffset(expected.RefreshDate.ToLocalTime());
            if (refreshDateTolerance == default)
            {
                Assert.That(actual.RefreshDate, Is.EqualTo(expectedDateTimeOffset));
            }
            else
            {
                Assert.That(actual.RefreshDate, Is.EqualTo(expectedDateTimeOffset).Within(refreshDateTolerance));
            }

            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.SourceUri.OriginalString, Is.EqualTo(expected.SourceUri));
            Assert.That(actual.SourceKind, Is.EqualTo((TorrentSourceKind)expected.SourceKind));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
        }
    }
}
