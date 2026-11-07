using System.Net;
using System.Net.Sockets;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Tests.Services.Transmission;

[Parallelizable(ParallelScope.All)]
internal sealed class TransmissionClientWrapperTests
{
    private const string _hashString = "0bda511316a069e86dd8ee8a3610475d2013a7fa";
    private const string _rpcError = "invalid or corrupt torrent file";
    private const string _connectionRefused = "Connection refused (transmission:9091)";

    private static readonly Uri _magnetUri = new($"magnet:?xt=urn:btih:{_hashString}");

    private static readonly Func<HttpRequestMessage, HttpResponseMessage> _unreachable = static _ =>
        throw new HttpRequestException(_connectionRefused, new SocketException((int)SocketError.ConnectionRefused));

    [TestCase("torrent-added", TransmissionAddTorrentResult.Added)]
    [TestCase("torrent-duplicate", TransmissionAddTorrentResult.Duplicate)]
    public async Task AddTorrentUsingMagnetAsync_WhenTransmissionNamesTheTorrent_ReturnsItWithoutError(
        string argument,
        TransmissionAddTorrentResult expected)
    {
        var (result, torrent, error) = await AddAsync(Json(
                $$$"""{"arguments":{"{{{argument}}}":{"hashString":"{{{_hashString}}}","id":1,"name":"TV Show 1"}},"result":"success"}"""))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(torrent?.HashString, Is.EqualTo(_hashString));
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public async Task AddTorrentUsingMagnetAsync_WhenSuccessNamesNoTorrent_ReturnsUnexpectedResponse()
    {
        var (result, torrent, error) = await AddAsync(Json("""{"arguments":{},"result":"success"}"""))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionAddTorrentResult.UnexpectedResponse));
            Assert.That(torrent, Is.Null);
            Assert.That(
                error,
                Is.EqualTo(
                    "Could not add a torrent to Transmission: its response holds neither torrent-added nor torrent-duplicate."));
        }
    }

    [Test]
    public async Task AddTorrentUsingMagnetAsync_WhenTransmissionReportsAnError_ReturnsRequestFailedWithTheReason()
    {
        var (result, torrent, error) = await AddAsync(Json($$"""{"arguments":{},"result":"{{_rpcError}}"}"""))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionAddTorrentResult.RequestFailed));
            Assert.That(torrent, Is.Null);
            Assert.That(error, Does.StartWith("Could not add a torrent to Transmission. ").And.Contain(_rpcError));
        }
    }

    [Test]
    public async Task GetTorrentAsync_WhenTransmissionHoldsTheTorrent_ReturnsItWithoutError()
    {
        var (result, torrent, error) = await GetAsync(Json(
                $$"""{"arguments":{"torrents":[{"hashString":"{{_hashString}}","name":"TV Show 1"}]},"result":"success"}"""))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionGetTorrentResult.Found));
            Assert.That(torrent?.HashString, Is.EqualTo(_hashString));
            Assert.That(error, Is.Null);
        }
    }

    [TestCase("""{"arguments":{"torrents":[]},"result":"success"}""")]
    [TestCase("""{"arguments":{},"result":"success"}""")]
    [TestCase("""{"result":"success"}""")]
    public async Task GetTorrentAsync_WhenTransmissionAnswersWithoutTheTorrent_ReturnsNotFound(string body)
    {
        var (result, torrent, error) = await GetAsync(Json(body)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionGetTorrentResult.NotFound));
            Assert.That(torrent, Is.Null);
            Assert.That(error, Is.EqualTo($"Could not get a torrent with hash '{_hashString}' from Transmission."));
        }
    }

    [Test]
    public async Task GetTorrentAsync_WhenTransmissionIsUnreachable_ReturnsRequestFailedWithTheReason()
    {
        var (result, torrent, error) = await GetAsync(_unreachable).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionGetTorrentResult.RequestFailed));
            Assert.That(torrent, Is.Null);
            Assert.That(
                error,
                Does.StartWith($"Could not get a torrent with hash '{_hashString}' from Transmission. ")
                    .And.Contain(_connectionRefused));
        }
    }

    [Test]
    public async Task RemoveTorrentAsync_WhenTransmissionRemovesTheTorrent_ReturnsRemovedWithoutError()
    {
        var (result, error) = await RemoveAsync(Json("""{"arguments":{},"result":"success"}""")).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionRemoveTorrentResult.Removed));
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public async Task RemoveTorrentAsync_WhenTransmissionIsUnreachable_ReturnsRequestFailedWithTheReason()
    {
        var (result, error) = await RemoveAsync(_unreachable).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TransmissionRemoveTorrentResult.RequestFailed));
            Assert.That(
                error,
                Does.StartWith($"Could not remove a torrent with hash '{_hashString}' from Transmission. ")
                    .And.Contain(_connectionRefused));
        }
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> Json(string body) =>
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };

    private static Task<TransmissionAddTorrentOutcome> AddAsync(Func<HttpRequestMessage, HttpResponseMessage> answer) =>
        CallAsync(answer, static w => w.AddTorrentUsingMagnetAsync(_magnetUri, "/tvshows", CancellationToken.None));

    private static Task<TransmissionGetTorrentOutcome> GetAsync(Func<HttpRequestMessage, HttpResponseMessage> answer) =>
        CallAsync(answer, static w => w.GetTorrentAsync(_hashString, CancellationToken.None));

    private static Task<TransmissionRemoveTorrentOutcome> RemoveAsync(
        Func<HttpRequestMessage, HttpResponseMessage> answer) =>
        CallAsync(answer, static w => w.RemoveTorrentAsync(_hashString, false, CancellationToken.None));

    private static async Task<TOutcome> CallAsync<TOutcome>(
        Func<HttpRequestMessage, HttpResponseMessage> answer,
        Func<TransmissionClientWrapper, Task<TOutcome>> call)
    {
        using var handler = new SequencedHttpMessageHandler();
        handler.Enqueue(answer);

        var options = new FakeOptionsMonitor<TransmissionClientOptions>(new()
        {
            BaseAddress = "http://transmission:9091",
            RpcEndpointAddressSuffix = "/transmission/rpc",
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new(options.CurrentValue.BaseAddress) };

        return await call(new TransmissionClientWrapper(new TransmissionClient(options, httpClient))).ConfigureAwait(false);
    }
}
