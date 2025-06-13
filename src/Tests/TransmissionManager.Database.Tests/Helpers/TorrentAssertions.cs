using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Tests.Helpers;

internal static class TorrentAssertions
{
    public static void AssertEqual(Torrent? actual, long expectedId, Torrent expected)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(expectedId));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
            Assert.That(actual.RefreshDate, Is.EqualTo(expected.RefreshDate));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(expected.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
        }
    }

    public static void AssertEqual(Torrent? actual, long expectedId, TorrentAddDto expected)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(expectedId));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
            Assert.That(actual.RefreshDate, Is.EqualTo(expected.RefreshDate));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(expected.WebPageUri.OriginalString));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
        }
    }

    public static void AssertEqual(Torrent? actual, long expectedId, TorrentUpdateDto expected)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(expectedId));
            if (expected.HashString is not null)
                Assert.That(actual.HashString, Is.EqualTo(expected.HashString));

            if (expected.RefreshDate is not null)
                Assert.That(actual.RefreshDate, Is.EqualTo(expected.RefreshDate.Value));

            if (expected.Name is not null)
                Assert.That(actual.Name, Is.EqualTo(expected.Name));

            if (expected.DownloadDir is not null)
                Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));

            if (expected.MagnetRegexPattern is not null)
            {
                if (string.IsNullOrEmpty(expected.MagnetRegexPattern))
                    Assert.That(actual.MagnetRegexPattern, Is.Null);
                else
                    Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            }

            if (expected.Cron is not null)
            {
                if (string.IsNullOrEmpty(expected.Cron))
                    Assert.That(actual.Cron, Is.Null);
                else
                    Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
            }
        }
    }
}
