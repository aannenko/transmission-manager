using System.Text;
using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class PaddedBytesReaderTests
{
    private static readonly byte[] _testBytes = Encoding.UTF8.GetBytes("abcdefghijklmnopqrst");

    [Test]
    public async Task ReadNextAsync_WhenSpecificPaddingIsUsed_ReadsNextBytes()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[10]);

        var result = await reader.ReadNextAsync(2).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("\0\0abcdefgh"u8), Is.True);
        }

        result = await reader.ReadNextAsync(2).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("ghijklmnop"u8), Is.True);
        }

        result = await reader.ReadNextAsync(4).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("mnopqrst"u8), Is.True);
        }

        result = await reader.ReadNextAsync(5).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False); // nothing new was read
            Assert.That(reader.Bytes.SequenceEqual("pqrst"u8), Is.True); // the retained window survives
        }

        result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0)); // nothing retained, nothing read
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenSpecificThenDefaultPaddingIsUsed_DoesNotIgnoreDefaultPadding()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[10]);

        var result = await reader.ReadNextAsync(2).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("\0\0abcdefgh"u8), Is.True);
        }

        result = await reader.ReadNextAsync(3).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("fghijklmno"u8), Is.True);
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenSpecificThenDefaultPaddingIsUsed_DoesNotThrowWhenStreamEnds()
    {
        using var stream = new MemoryStream("abcde"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[5]);

        var result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("abcde"u8), Is.True);
        }

        result = await reader.ReadNextAsync(3).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.SequenceEqual("cde"u8), Is.True);
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenPaddingIsTheSizeOfBytesSpan_DoesNotThrow()
    {
        using var stream = new MemoryStream("abcde"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[10]);

        var result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("abcde"u8), Is.True);
        }

        result = await reader.ReadNextAsync(5).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.SequenceEqual("abcde"u8), Is.True);
        }

        result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }
    }

    /// <remarks>
    /// Pins the guarantee the magnet scan depends on: a caller may retain a window, discover the
    /// stream is exhausted, and still read that window back. Losing it silently discarded magnets
    /// that were already fully buffered.
    /// </remarks>
    [Test]
    public async Task ReadNextAsync_WhenNothingNewIsReadAfterRetainingBytes_KeepsTheRetainedWindow()
    {
        using var stream = new MemoryStream("abcdefgh"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[10]);

        _ = await reader.ReadNextAsync(0).ConfigureAwait(false);

        var result = await reader.ReadNextAsync(6).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.SequenceEqual("cdefgh"u8), Is.True);
        }
    }

    [Test]
    public void Constructor_WhenStreamIsNull_ThrowsArgumentNullException()
    {
        Assert.That(() => new PaddedBytesReader(null!, new byte[5]), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_WhenBufferIsNull_ThrowsArgumentNullException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_WhenBufferIsZeroLength_ThrowsArgumentException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, []), Throws.ArgumentException);
    }

    [Test]
    public async Task ReadNextAsync_WhenPaddingIsEqualToBufferSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[5]);

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(5).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }

    [Test]
    public async Task ReadNextAsync_WhenPaddingIsGreaterThanBufferSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[5]);

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(6).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }

    [Test]
    public async Task ReadNextAsync_WhenPaddingIsGreaterThanBytesSpanSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream("abcde"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[10]);

        var result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("abcde"u8), Is.True);
        }

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(6).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }

    [Test]
    public async Task ReadNextAsync_WhenPaddingIsNegative_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[5]);

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(-1).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }
}
