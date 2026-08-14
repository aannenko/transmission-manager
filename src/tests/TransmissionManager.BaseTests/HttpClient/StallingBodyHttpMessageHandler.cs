using System.Net;
using System.Net.Sockets;

namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Answers with response headers immediately and then never delivers the body.
/// </summary>
/// <remarks>
/// The shape <see cref="FakeHttpMessageHandler"/> cannot express, since it always completes its
/// content. Nothing but a caller-supplied deadline ends a request answered this way: a resilience
/// pipeline's timeouts elapse once the headers arrive, and it leaves
/// <c>HttpClient.Timeout</c> infinite.
/// <para>
/// An aborted read is reported the way <c>SocketsHttpHandler</c> reports it: a
/// <see cref="TaskCanceledException"/> carrying the transport failure as its inner exception.
/// </para>
/// </remarks>
public sealed class StallingBodyHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StallingStream()),
        });
    }

    private sealed class StallingStream : Stream
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

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TaskCanceledException(
                    "The read was aborted.",
                    new IOException(
                        "Unable to read data from the transport connection.",
                        new SocketException((int)SocketError.OperationAborted)),
                    cancellationToken);
            }

            return 0;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
