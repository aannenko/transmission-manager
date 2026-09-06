using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Tests.Extensions;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentDtoExtensionsTests
{
    [Test]
    public void ApplyPatch_WhenJsonValueFormatIsChanged_ReplaysTheNewValue()
    {
        var torrent = CreateTorrent("magnet:?xt=urn:btih:{0}");
        var patch = new UpdateTorrentByIdRequest
        {
            JsonValueFormat = "magnet:?xt=urn:btih:{0}&tr=https%3A%2F%2Ftracker.example%2Fannounce",
        };

        var updated = torrent.ApplyPatch(patch);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.JsonValueFormat, Is.EqualTo(patch.JsonValueFormat));
            Assert.That(updated.Version, Is.EqualTo(torrent.Version + 1));
        }
    }

    [Test]
    public void ApplyPatch_WhenJsonValueFormatIsCleared_ReplaysNull()
    {
        var torrent = CreateTorrent("magnet:?xt=urn:btih:{0}");
        var patch = new UpdateTorrentByIdRequest { JsonValueFormat = string.Empty };

        var updated = torrent.ApplyPatch(patch);

        Assert.That(updated.JsonValueFormat, Is.Null);
    }

    private static TorrentDto CreateTorrent(string jsonValueFormat) =>
        new(
            Id: 1,
            HashString: "0BDA511316A069E86DD8EE8A3610475D2013A7FA",
            RefreshDate: new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero),
            Name: "Torrent",
            SourceUri: new("https://api.example/topics#/result/1/hash"),
            SourceKind: TorrentSourceKind.JsonPointer,
            DownloadDir: "/tvshows",
            MagnetRegexPattern: "[a-fA-F0-9]{40}",
            JsonValueFormat: jsonValueFormat,
            Cron: null,
            Version: 3);
}
