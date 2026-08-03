namespace TransmissionManager.TorrentSources.Utils;

internal sealed class PaddedBytesReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private readonly int _maxBufferFreeSpace;

    private int _bytesLength;

    public PaddedBytesReader(Stream stream, byte[] buffer, int maxBufferFreeSpace = 0)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        if (buffer.Length is 0)
            throw new ArgumentException($"The {nameof(buffer)} size must be greater than 0.", nameof(buffer));

        _maxBufferFreeSpace = maxBufferFreeSpace >= 0 && maxBufferFreeSpace < _buffer.Length
            ? maxBufferFreeSpace
            : throw new ArgumentOutOfRangeException(
                nameof(maxBufferFreeSpace),
                $"The value must be greater than or equal to 0 and less than the {nameof(buffer)} size.");

        _bytesLength = buffer.Length;
    }

    public ReadOnlySpan<byte> Bytes => new(_buffer, 0, _bytesLength);

    /// <summary>
    /// Reads the next chunk of bytes from the stream into the buffer, shifting a <paramref name="padding"/>
    /// of bytes from the end of the current <see cref="Bytes"/> span to the start of the buffer with each read.
    /// </summary>
    /// <param name="padding">The number of bytes to shift from the end of the current <see cref="Bytes"/> span.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// Whether any new bytes were read from the stream. A <see langword="false"/> result means the stream is exhausted,
    /// not that the reader is empty: <see cref="Bytes"/> still exposes the retained <paramref name="padding"/> bytes.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the padding is negative, exceeds the current buffer length, or equals the buffer size.
    /// </exception>
    public async ValueTask<bool> ReadNextAsync(int padding, CancellationToken cancellationToken = default)
    {
        if (padding > _bytesLength || padding == _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(padding),
                $"The value must be between 0 and the length of the {nameof(Bytes)} span.");
        }

        if (padding > 0)
            Bytes[^padding..].CopyTo(_buffer);

        _bytesLength = padding;

        int read;
        do
        {
            read = await _stream.ReadAsync(_buffer.AsMemory(_bytesLength), cancellationToken).ConfigureAwait(false);
            _bytesLength += read;
        }
        while (read > 0 && _buffer.Length - _bytesLength > _maxBufferFreeSpace);

        return _bytesLength > padding;
    }
}
