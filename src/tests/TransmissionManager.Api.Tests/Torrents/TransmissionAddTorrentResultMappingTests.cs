using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Transmission;

namespace TransmissionManager.Api.Tests.Torrents;

[Parallelizable(ParallelScope.All)]
internal sealed class TransmissionAddTorrentResultMappingTests
{
    private static readonly TransmissionAddResult[] _apiResults = Enum.GetValues<TransmissionAddResult>();

    private static readonly TransmissionAddTorrentResult[] _resultsWithoutApiCounterpart = Enum
        .GetValues<TransmissionAddTorrentResult>()
        .Where(static r => !Enum.IsDefined(typeof(TransmissionAddResult), r.ToString()))
        .ToArray();

    [Test]
    public void TransmissionAddTorrentResult_WhenComparedToTheApiEnum_IsASupersetOfIt() =>
        Assert.That(
            Enum.GetNames<TransmissionAddTorrentResult>(),
            Is.SupersetOf(Enum.GetNames<TransmissionAddResult>()));

    [TestCaseSource(nameof(_apiResults))]
    public void ToTransmissionAddResult_WhenMemberHasAnApiCounterpart_ReturnsIt(TransmissionAddResult expected)
    {
        var result = Enum.Parse<TransmissionAddTorrentResult>(expected.ToString());

        Assert.That(result.ToTransmissionAddResult(), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(_resultsWithoutApiCounterpart))]
    public void ToTransmissionAddResult_WhenMemberHasNoApiCounterpart_ThrowsArgumentOutOfRangeException(
        TransmissionAddTorrentResult result) =>
        Assert.That(() => result.ToTransmissionAddResult(), Throws.TypeOf<ArgumentOutOfRangeException>());
}
