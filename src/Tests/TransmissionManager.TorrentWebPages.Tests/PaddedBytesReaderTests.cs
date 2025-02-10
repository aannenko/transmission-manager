using System.Text;
using TransmissionManager.TorrentWebPages.Utils;

namespace TransmissionManager.TorrentWebPages.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class PaddedBytesReaderTests
{
    private static readonly byte[] _testBytes = Encoding.UTF8.GetBytes("abcdefghijklmnopqrst");

    [Test]
    public async Task ReadNextAsync_WhenGivenDefaultPadding_ReadsNextBytes()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[10], 3);

        var result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("abcdefghij"u8), Is.True);
        }

        result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("hijklmnopq"u8), Is.True);
        }

        result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("opqrst"u8), Is.True);
        }

        result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }

        result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenSpecificPadding_ReadsNextBytes()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[10], 0);

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
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }

        result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenSpecificThenDefaultPadding_DoesNotIgnoreDefaultPadding()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[10], 3);

        var result = await reader.ReadNextAsync(2).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("\0\0abcdefgh"u8), Is.True);
        }

        result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("fghijklmno"u8), Is.True);
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenSpecificThenDefaultPadding_DoesNotThrowWhenStreamEnds()
    {
        using var stream = new MemoryStream("abcde"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[5], 3);

        var result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("abcde"u8), Is.True);
        }

        result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenPaddingEqualToBytesSpanSize_DoesNotThrow()
    {
        using var stream = new MemoryStream("abcde"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[10], 2);

        var result = await reader.ReadNextAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(reader.Bytes.SequenceEqual("abcde"u8), Is.True);
        }

        result = await reader.ReadNextAsync(5).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }

        result = await reader.ReadNextAsync(0).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(reader.Bytes.Length, Is.EqualTo(0));
        }
    }

    [Test]
    public void Constructor_WhenGivenNullStream_ThrowsArgumentNullException()
    {
        Assert.That(() => new PaddedBytesReader(null!, new byte[5], 0), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_WhenGivenNullBuffer_ThrowsArgumentNullException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, null!, 0), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_WhenGivenZeroLengthBuffer_ThrowsArgumentException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, [], 0), Throws.ArgumentException);
    }

    [Test]
    public void Constructor_WhenGivenPaddingEqualToBufferSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, new byte[5], 5), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_WhenGivenPaddingGreaterThanBufferSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, new byte[5], 6), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_WhenGivenNegativePadding_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        Assert.That(() => new PaddedBytesReader(stream, new byte[5], -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenPaddingEqualToBufferSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[5], 2);

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(5).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenPaddingGreaterThanBufferSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[5], 2);

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(6).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }

    [Test]
    public async Task ReadNextAsync_WhenGivenPaddingGreaterThanBytesSpanSize_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream("abcde"u8.ToArray());
        var reader = new PaddedBytesReader(stream, new byte[10], 2);

        var result = await reader.ReadNextAsync().ConfigureAwait(false);
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
    public async Task ReadNextAsync_WhenGivenNegativePadding_ThrowsArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(_testBytes);
        var reader = new PaddedBytesReader(stream, new byte[5], 2);

        await Assert.ThatAsync(
            async () => await reader.ReadNextAsync(-1).ConfigureAwait(false),
            Throws.InstanceOf<ArgumentOutOfRangeException>()).ConfigureAwait(false);
    }
}
