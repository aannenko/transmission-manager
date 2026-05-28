using TransmissionManager.Api.Actions.Torrents.GetPage;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.Tests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class GetTorrentPageOrderMappingTests
{
    [Test]
    public void GetTorrentPageOrder_And_TorrentOrder_HaveMatchingNamesAndValues()
    {
        var apiNames = Enum.GetNames<GetTorrentPageOrder>();
        var dbNames = Enum.GetNames<TorrentOrder>();

        Assert.That(apiNames, Is.EqualTo(dbNames));

        var apiValues = Enum.GetValues<GetTorrentPageOrder>().Select(v => (int)v).ToArray();
        var dbValues = Enum.GetValues<TorrentOrder>().Select(v => (int)v).ToArray();

        Assert.That(apiValues, Is.EqualTo(dbValues));
    }

    [Test]
    public void GetTorrentPageOrder_And_TorrentOrder_HaveMatchingIsDescendingClassification()
    {
        var apiValues = Enum.GetValues<GetTorrentPageOrder>();
        var dbValues = Enum.GetValues<TorrentOrder>();

        Assert.That(apiValues, Has.Length.EqualTo(dbValues.Length));

        using (Assert.EnterMultipleScope())
        {
            for (var i = 0; i < apiValues.Length; i++)
            {
                Assert.That(
                    apiValues[i].IsDescending(),
                    Is.EqualTo(dbValues[i].IsDescending()),
                    $"IsDescending mismatch for {apiValues[i]} (API) vs {dbValues[i]} (DB).");
            }
        }
    }
}
