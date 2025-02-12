namespace TransmissionManager.TorrentWebPages.Utils;

internal sealed class PaddedBytesReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;

    private int _bytesLength;

    public PaddedBytesReader(Stream stream, byte[] buffer)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

        if (buffer.Length is 0)
            throw new ArgumentException("The buffer size must be greater than 0.", nameof(buffer));

        _bytesLength = _buffer.Length;
    }

    public ReadOnlySpan<byte> Bytes => _buffer.AsSpan(0, _bytesLength);

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
        bool wereBytesRead = false;

        int read;
        do
        {
            read = await _stream.ReadAsync(_buffer.AsMemory(_bytesLength), cancellationToken).ConfigureAwait(false);
            wereBytesRead = wereBytesRead || read > 0;
            _bytesLength += read;
        } while (read > 0 && _bytesLength < _buffer.Length);

        if (!wereBytesRead)
            _bytesLength = 0;

        return wereBytesRead;
    }
}
