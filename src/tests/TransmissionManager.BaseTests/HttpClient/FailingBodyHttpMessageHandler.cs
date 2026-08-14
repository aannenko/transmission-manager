using System.Net;
using System.Net.Sockets;

namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Answers with response headers immediately and then fails the body read with an
/// <see cref="IOException"/>, as a source that drops the connection mid-response does.
/// </summary>
public sealed class FailingBodyHttpMessageHandler : HttpMessageHandler
{
    public const string ErrorMessage = "Unable to read data from the transport connection.";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new FailingStream()),
        });
    }

    private sealed class FailingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            throw new IOException(ErrorMessage, new SocketException((int)SocketError.ConnectionReset));

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
