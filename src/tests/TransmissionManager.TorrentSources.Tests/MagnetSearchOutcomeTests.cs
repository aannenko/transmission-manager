using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class MagnetSearchOutcomeTests
{
    [Test]
    public void Found_WhenGivenAMagnetUri_CarriesItAndNoError()
    {
        var magnetUri = new Uri("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2");

        var outcome = MagnetSearchOutcome.Found(magnetUri);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(magnetUri));
            Assert.That(outcome.Error, Is.Null);
        }
    }

    [TestCase(MagnetSearchResult.NotFound)]
    [TestCase(MagnetSearchResult.RetrievalFailed)]
    [TestCase(MagnetSearchResult.InvalidSource)]
    [TestCase(MagnetSearchResult.InvalidSelector)]
    public void Failure_WhenGivenANonFoundResult_CarriesNoMagnetUri(MagnetSearchResult result)
    {
        var outcome = MagnetSearchOutcome.Failure(result, "detail");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(result));
            Assert.That(outcome.MagnetUri, Is.Null);
            Assert.That(outcome.Error, Is.EqualTo("detail"));
        }
    }

    [Test]
    public void Failure_WhenGivenFound_Throws()
    {
        Assert.That(
            () => MagnetSearchOutcome.Failure(MagnetSearchResult.Found, "detail"),
            Throws.TypeOf<ArgumentException>());
    }
}
