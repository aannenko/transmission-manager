using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.Api.Tests.Torrents;

[Parallelizable(ParallelScope.All)]
internal sealed class MagnetSearchResultExtensionsTests
{
    private static readonly MagnetSearchResult[] _unprocessable =
        [MagnetSearchResult.InvalidSource, MagnetSearchResult.InvalidSelector, MagnetSearchResult.NotFound];

    private static readonly MagnetSearchResult[] _dependencyFailures = [MagnetSearchResult.RetrievalFailed];

    /// <remarks>
    /// <see cref="MagnetSearchResult.NotFound"/> is the non-obvious member: the source answered
    /// successfully, so the dependency did its job and only the extraction produced nothing. Its own
    /// remarks state that repeating the search changes nothing until the source changes, which is a
    /// 4xx, not the "try again" a 424 implies.
    /// </remarks>
    [Test]
    public void IsUnprocessableSource_WhenTheSourceAnsweredButNoMagnetCameOfIt_ReturnsTrue() =>
        Assert.That(_unprocessable, Is.All.Matches<MagnetSearchResult>(static r => r.IsUnprocessableSource()));

    [Test]
    public void IsUnprocessableSource_WhenTheSourceCouldNotBeRead_ReturnsFalse() =>
        Assert.That(_dependencyFailures, Is.All.Matches<MagnetSearchResult>(static r => !r.IsUnprocessableSource()));

    /// <remarks>
    /// An unclassified member falls through to a dependency failure, which is the safe direction - a
    /// 424 invites a retry, whereas a wrong 4xx tells the caller to change a configuration that may
    /// be correct. Adding a member fails this until it is listed above, and listing it there is what
    /// the two assertions above run through <see cref="MagnetSearchResultExtensions"/> - so a
    /// classification cannot be claimed here without the predicate implementing it.
    /// </remarks>
    [Test]
    public void IsUnprocessableSource_WhenEveryMemberIsClassified_CoversTheWholeEnum()
    {
        var classified = _unprocessable
            .Concat(_dependencyFailures)
            .Append(MagnetSearchResult.Found)
            .ToArray();

        Assert.That(Enum.GetValues<MagnetSearchResult>(), Is.EquivalentTo(classified));
    }
}
