using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Polly.Timeout;
using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Transmission.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class ResponseBufferingHandlerTests
{
    private const string _hash = "0bda511316a069e86dd8ee8a3610475d2013a7fa";
    private const string _resilienceOptionsName = $"{nameof(TransmissionClient)}-standard";

    private const string _torrentGetResponse =
        $$"""{"arguments":{"torrents":[{"hashString":"{{_hash}}","name":"TV Show 1"}]},"result":"success"}""";

    private static readonly TimeSpan _hangGuard = TimeSpan.FromSeconds(10);

    [Test]
    public async Task GetTorrentsAsync_WhenEveryResponseBodyStalls_ThrowsHttpRequestExceptionWithinTheTotalRequestTimeout()
    {
        var time = new FakeTimeProvider();
        var firstSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transmission = new SequencedHttpMessageHandler();
        var provider = CreateProvider(transmission, time);
        try
        {
            var resilience = GetResilienceOptions(provider);

            transmission.Enqueue(_ => CreateStalledResponse(firstSent));
            for (var retry = 0; retry < resilience.Retry.MaxRetryAttempts; retry++)
                transmission.Enqueue(static _ => CreateStalledResponse());

            var request = provider.GetRequiredService<TransmissionClient>().GetTorrentsAsync([_hash]);
            await firstSent.Task.WaitAsync(_hangGuard).ConfigureAwait(false);
            time.Advance(resilience.TotalRequestTimeout.Timeout);

            await Assert.ThatAsync(
                async () => await request.WaitAsync(_hangGuard).ConfigureAwait(false),
                Throws.TypeOf<HttpRequestException>().With.InnerException.TypeOf<TimeoutRejectedException>())
                .ConfigureAwait(false);
        }
        finally
        {
            await DisposeWithinHangGuardAsync(provider).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task GetTorrentsAsync_WhenOnlyTheFirstResponseBodyStalls_RetriesAndReturnsTheNextResponse()
    {
        var time = new FakeTimeProvider();
        var firstSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transmission = new SequencedHttpMessageHandler();

        // The default retry delay is jittered, so the clock could not be advanced past it without guessing.
        var provider = CreateProvider(transmission, time, static options =>
        {
            options.Retry.UseJitter = false;
            options.Retry.Delay = TimeSpan.Zero;
        });

        try
        {
            transmission.Enqueue(_ => CreateStalledResponse(firstSent));
            transmission.Enqueue(static _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_torrentGetResponse),
            });

            var request = provider.GetRequiredService<TransmissionClient>().GetTorrentsAsync([_hash]);
            await firstSent.Task.WaitAsync(_hangGuard).ConfigureAwait(false);
            time.Advance(GetResilienceOptions(provider).AttemptTimeout.Timeout);

            var response = await request.WaitAsync(_hangGuard).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Arguments?.Torrents?.Single().HashString, Is.EqualTo(_hash));
                Assert.That(transmission.CallCount, Is.EqualTo(2));
            }
        }
        finally
        {
            await DisposeWithinHangGuardAsync(provider).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task GetTorrentsAsync_WhenCallerCancelsWhileTheResponseBodyStalls_ThrowsOperationCanceledException()
    {
        var firstSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transmission = new SequencedHttpMessageHandler();
        using var callerCts = new CancellationTokenSource();
        var provider = CreateProvider(transmission, new FakeTimeProvider());
        try
        {
            transmission.Enqueue(_ => CreateStalledResponse(firstSent));

            var request = provider
                .GetRequiredService<TransmissionClient>()
                .GetTorrentsAsync([_hash], cancellationToken: callerCts.Token);

            await firstSent.Task.WaitAsync(_hangGuard).ConfigureAwait(false);
            await callerCts.CancelAsync().ConfigureAwait(false);

            await Assert.ThatAsync(
                async () => await request.WaitAsync(_hangGuard).ConfigureAwait(false),
                Throws.InstanceOf<OperationCanceledException>())
                .ConfigureAwait(false);
        }
        finally
        {
            await DisposeWithinHangGuardAsync(provider).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task SendAsync_WhenBufferingTheBodyFails_DisposesTheResponse()
    {
        using var content = new UnreadableContent();
        using var transmission = new SequencedHttpMessageHandler();
        using var bufferingHandler = new ResponseBufferingHandler { InnerHandler = transmission };
        using var invoker = new HttpMessageInvoker(bufferingHandler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://transmission:9091/transmission/rpc");

        transmission.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

        await Assert.ThatAsync(
            async () => await invoker.SendAsync(request, CancellationToken.None).ConfigureAwait(false),
            Throws.InstanceOf<HttpRequestException>())
            .ConfigureAwait(false);

        Assert.That(content.IsDisposed, Is.True);
    }

    private static ServiceProvider CreateProvider(
        HttpMessageHandler transmission,
        TimeProvider time,
        Action<HttpStandardResilienceOptions>? configureResilience = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Transmission:BaseAddress"] = "http://transmission:9091",
                ["Transmission:RpcEndpointAddressSuffix"] = "/transmission/rpc",
                ["Transmission:SessionHeaderName"] = "X-Transmission-Session-Id",
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton(time)
            .AddTransmissionServices(configuration);

        _ = services
            .AddHttpClient<TransmissionClient>()
            .ConfigurePrimaryHttpMessageHandler(() => transmission);

        if (configureResilience is not null)
            _ = services.PostConfigure(_resilienceOptionsName, configureResilience);

        return services.BuildServiceProvider();
    }

    private static HttpStandardResilienceOptions GetResilienceOptions(ServiceProvider provider) =>
        provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>().Get(_resilienceOptionsName);

    private static HttpResponseMessage CreateStalledResponse(TaskCompletionSource? sent = null)
    {
        _ = sent?.TrySetResult();
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallingStream()) };
    }

    /// <remarks>
    /// The resilience pipeline's disposal waits on the fake clock for executions still in flight, so an execution a
    /// regression leaves stuck would hang the run instead of failing the test.
    /// </remarks>
    private static async Task DisposeWithinHangGuardAsync(ServiceProvider provider)
    {
        var disposal = provider.DisposeAsync().AsTask();
        _ = await Task.WhenAny(disposal, Task.Delay(_hangGuard)).ConfigureAwait(false);
    }

    private sealed class UnreadableContent : HttpContent
    {
        public bool IsDisposed { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.FromException(new IOException("Unable to read data from the transport connection."));

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
