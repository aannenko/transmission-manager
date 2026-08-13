using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.Database.Models;
using ApiSourceKind = TransmissionManager.Api.Common.Dto.Torrents.TorrentSourceKind;
using DbSourceKind = TransmissionManager.Database.Dto.TorrentSourceKind;

namespace TransmissionManager.Api.Tests.Torrents;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentSourceKindMappingTests
{
    [Test]
    public void TorrentSourceKind_WhenComparedAcrossProjects_HasMatchingNamesAndValues()
    {
        var apiNames = Enum.GetNames<ApiSourceKind>();
        var dbNames = Enum.GetNames<DbSourceKind>();

        Assert.That(apiNames, Is.EqualTo(dbNames));

        var apiValues = Enum.GetValues<ApiSourceKind>().Select(static v => (int)v).ToArray();
        var dbValues = Enum.GetValues<DbSourceKind>().Select(static v => (int)v).ToArray();

        Assert.That(apiValues, Is.EqualTo(dbValues));
    }

    /// <remarks>
    /// The values are persisted as integers, so reassigning one silently reinterprets every stored
    /// row. Pinning them makes that a test failure rather than a data corruption.
    /// </remarks>
    [Test]
    public void TorrentSourceKind_WhenPersisted_KeepsItsStorageContract()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That((int)DbSourceKind.WebPage, Is.Zero);
            Assert.That((int)DbSourceKind.JsonPointer, Is.EqualTo(1));
        }
    }

    [TestCase(DbSourceKind.WebPage, ApiSourceKind.WebPage)]
    [TestCase(DbSourceKind.JsonPointer, ApiSourceKind.JsonPointer)]
    public void ToDto_WhenTorrentHasSourceKind_MapsItToTheApiCounterpart(
        DbSourceKind stored,
        ApiSourceKind expected)
    {
        var torrent = new Torrent
        {
            Id = 1,
            HashString = "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            RefreshDate = new(2024, 12, 3, 10, 20, 30, DateTimeKind.Utc),
            Name = "TV show name",
            SourceUri = "https://torrenttracker.com/api/1106#/result/6880555/7",
            SourceKind = stored,
            DownloadDir = "/tvshows",
            Version = 1,
        };

        var dto = torrent.ToDto();

        Assert.That(dto.SourceKind, Is.EqualTo(expected));
    }
}
