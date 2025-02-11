namespace TransmissionManager.TorrentWebPages.Utils;

internal sealed class PaddedBytesReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private readonly int _defaultPadding;

    private int _padding;
    private int _bytesLength;

    public PaddedBytesReader(Stream stream, byte[] buffer, int defaultPadding)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

        if (buffer.Length is 0)
            throw new ArgumentException("The buffer size must be greater than 0.", nameof(buffer));

        if (defaultPadding < 0 || defaultPadding >= buffer.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultPadding),
                "The value must greater than or equal to 0 and smaller than the buffer size.");
        }

        _defaultPadding = defaultPadding;
        _bytesLength = _buffer.Length;
    }

    public ReadOnlySpan<byte> Bytes => _buffer.AsSpan(0, _bytesLength);

    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default) =>
        ReadNextAsync(_padding, cancellationToken);

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

        _padding = Math.Min(_defaultPadding, _bytesLength);

        return wereBytesRead;
    }
}
