using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class GetTorrentPageDirectionMappingTests
{
    [Test]
    public void GetTorrentPageDirection_And_PaginationDirection_Have_Matching_Names_And_Values()
    {
        var apiNames = Enum.GetNames<GetTorrentPageDirection>();
        var dbNames = Enum.GetNames<PaginationDirection>();

        Assert.That(apiNames, Is.EqualTo(dbNames));

        var apiValues = Enum.GetValues<GetTorrentPageDirection>().Select(v => (int)v).ToArray();
        var dbValues = Enum.GetValues<PaginationDirection>().Select(v => (int)v).ToArray();

        Assert.That(apiValues, Is.EqualTo(dbValues));
    }
}
