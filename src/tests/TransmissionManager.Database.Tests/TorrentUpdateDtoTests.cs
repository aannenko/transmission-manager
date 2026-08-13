using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentUpdateDtoTests
{
    [Test]
    public void Constructor_WhenAllFieldsAreNull_ThrowsArgumentException()
    {
        Assert.That(static () => new TorrentUpdateDto(), Throws.ArgumentException);
    }
}
