using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Answers with response headers immediately and then never delivers the body.
/// </summary>
/// <param name="abortAsIoException">
/// Whether an aborted read fails with an <see cref="IOException"/> instead of an
/// <see cref="OperationCanceledException"/>, as it can in practice.
/// </param>
/// <remarks>
/// The shape <see cref="FakeHttpMessageHandler"/> cannot express, since it always completes its
/// content. Nothing but a caller-supplied deadline ends a request answered this way: a resilience
/// pipeline's timeouts elapse once the headers arrive, and it leaves
/// <c>HttpClient.Timeout</c> infinite.
/// </remarks>
public sealed class StallingBodyHttpMessageHandler(bool abortAsIoException = false) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StallingStream(abortAsIoException)),
        });
    }

    private sealed class StallingStream(bool abortAsIoException) : Stream
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
            catch (OperationCanceledException) when (abortAsIoException)
            {
                throw new IOException("Unable to read data from the transport connection.");
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
