using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class GetTorrentPageOrderMappingTests
{
    [Test]
    public void GetTorrentPageOrder_And_TorrentOrder_Have_Matching_Names_And_Values()
    {
        var apiNames = Enum.GetNames<GetTorrentPageOrder>();
        var dbNames = Enum.GetNames<TorrentOrder>();

        Assert.That(apiNames, Is.EqualTo(dbNames));

        var apiValues = Enum.GetValues<GetTorrentPageOrder>().Select(v => (int)v).ToArray();
        var dbValues = Enum.GetValues<TorrentOrder>().Select(v => (int)v).ToArray();

        Assert.That(apiValues, Is.EqualTo(dbValues));
    }
}
