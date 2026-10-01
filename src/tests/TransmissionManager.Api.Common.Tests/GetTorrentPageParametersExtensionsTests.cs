using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Common.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class GetTorrentPageParametersExtensionsTests
{
    /// <remarks>
    /// Values equal to a default are left out rather than written, so the estimate that skips them
    /// and the writer that skips them cannot drift apart. <c>take</c> is the exception - it is
    /// always written, and the 20 here is the primary constructor's default rather than a value
    /// this test supplied.
    /// </remarks>
    [Test]
    public void ToPathAndQueryString_WhenValuesAreLeftAtTheirDefaults_WritesTakeAlone()
    {
        var parameters = new GetTorrentPageParameters(
            OrderBy: GetTorrentPageOrder.Id,
            AnchorValue: string.Empty,
            Direction: GetTorrentPageDirection.Forward,
            PropertyStartsWith: string.Empty);

        Assert.That(
            parameters.ToPathAndQueryString(),
            Is.EqualTo($"{EndpointAddresses.Torrents}?take=20"));
    }

    /// <remarks>
    /// An under-estimate of the rented size is invisible here: the builder grows and re-copies, so
    /// only the bucket differs, never the string.
    /// </remarks>
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    [TestCase(-1000000000000000000L)]
    [TestCase(0L)]
    public void ToPathAndQueryString_WhenAnchorIdIsAtALimit_WritesEveryDigitOfIt(long anchorId)
    {
        var parameters = new GetTorrentPageParameters(AnchorId: anchorId, Take: GetTorrentPageParameters.MaxTake);

        Assert.That(
            parameters.ToPathAndQueryString(),
            Is.EqualTo(
                $"{EndpointAddresses.Torrents}?take={GetTorrentPageParameters.MaxTake}&anchorId={anchorId}"));
    }

    [Test]
    public void ToPathAndQueryString_WhenEveryValueIsSet_WritesThemInOrderAndEscaped()
    {
        var parameters = new GetTorrentPageParameters(
            OrderBy: GetTorrentPageOrder.DownloadDirDesc,
            AnchorId: long.MinValue,
            AnchorValue: "Tom & Jerry",
            Take: GetTorrentPageParameters.MaxTake,
            Direction: GetTorrentPageDirection.Backward,
            PropertyStartsWith: "a/b c",
            CronExists: false);

        Assert.That(parameters.ToPathAndQueryString(), Is.EqualTo(
            $"{EndpointAddresses.Torrents}?take=10000&orderBy=DownloadDirDesc&anchorId=-9223372036854775808" +
            "&anchorValue=Tom+%26+Jerry&direction=Backward&propertyStartsWith=a%2Fb+c&cronExists=False"));
    }
}
