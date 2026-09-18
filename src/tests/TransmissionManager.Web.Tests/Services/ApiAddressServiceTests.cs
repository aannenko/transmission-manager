using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Services;
using TransmissionManager.Web.Tests.Helpers;

namespace TransmissionManager.Web.Tests.Services;

[Parallelizable(ParallelScope.Self)]
internal sealed class ApiAddressServiceTests
{
    private const string _noRequestExpected = "This test expects no request.";
    private const string _storageKey = "baseAddress";
    private const string _hostAddress = "http://localhost:5000/";
    private const string _storedAddress = "http://stored.example:9092/";
    private const string _appVersionAddress = "http://api.example:9092/api/v1/appversion";

    private static readonly Uri _apiAddress = new("http://api.example:9092/");
    private static readonly Uri _defaultAddress = new("http://localhost:9092/");

    [Test]
    public void BaseAddress_BeforeAnythingIsLoaded_IsTheHostAddressOnTheApiPort()
    {
        using var handler = new ThrowingHttpMessageHandler(_noRequestExpected);

        var service = CreateService(new(), handler);

        Assert.That(service.BaseAddress, Is.EqualTo(_defaultAddress));
    }

    [TestCase("http://api.example:9092/")]
    [TestCase("https://api.example/")]
    public async Task LoadAsync_WhenStorageHoldsAnHttpAddress_AdoptsIt(string stored)
    {
        using var handler = new ThrowingHttpMessageHandler(_noRequestExpected);
        var runtime = new FakeJSRuntime();
        runtime.Storage[_storageKey] = stored;
        var service = CreateService(runtime, handler);

        await service.LoadAsync().ConfigureAwait(false);

        Assert.That(service.BaseAddress, Is.EqualTo(new Uri(stored)));
    }

    [TestCase(null, TestName = "LoadAsync_WhenStorageHoldsNoHttpAddress_KeepsTheDefault(nothing stored)")]
    [TestCase("", TestName = "LoadAsync_WhenStorageHoldsNoHttpAddress_KeepsTheDefault(empty string)")]
    [TestCase("   ", TestName = "LoadAsync_WhenStorageHoldsNoHttpAddress_KeepsTheDefault(blank)")]
    [TestCase("not an address")]
    [TestCase("/api/v1/torrents")]
    [TestCase("localhost:9092")]
    [TestCase("ftp://api.example/")]
    [TestCase(@"C:\temp")]
    public async Task LoadAsync_WhenStorageHoldsNoHttpAddress_KeepsTheDefault(string? stored)
    {
        using var handler = new ThrowingHttpMessageHandler(_noRequestExpected);
        var runtime = new FakeJSRuntime();
        if (stored is not null)
            runtime.Storage[_storageKey] = stored;

        var service = CreateService(runtime, handler);

        await service.LoadAsync().ConfigureAwait(false);

        Assert.That(service.BaseAddress, Is.EqualTo(_defaultAddress));
    }

    [Test]
    public async Task ConnectAsync_WhenTheApiAnswersAsItself_RemembersTheAddress()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, new(_appVersionAddress)),
            new(HttpStatusCode.OK, Content: "\"1.2.3\""));

        var runtime = new FakeJSRuntime();
        var service = CreateService(runtime, handler);

        var result = await service.ConnectAsync(_apiAddress).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Success));
            Assert.That(result.Value, Is.EqualTo(new Version(1, 2, 3)));
            Assert.That(service.BaseAddress, Is.EqualTo(_apiAddress));
            Assert.That(runtime.Storage[_storageKey], Is.EqualTo(_apiAddress.AbsoluteUri));
        }
    }

    [TestCase(HttpStatusCode.NotFound, """{"status":404,"errors":{"id":["No such endpoint."]}}""")]
    [TestCase(HttpStatusCode.OK, "<html>Some other application</html>")]
    [TestCase(HttpStatusCode.BadGateway, "<html>Proxy failure</html>")]
    public async Task ConnectAsync_WhenTheAddressAnswersButNotAsThisApi_KeepsThePreviousAddress(
        HttpStatusCode statusCode,
        string content)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, new(_appVersionAddress)),
            new(statusCode, Content: content));

        var runtime = new FakeJSRuntime();
        runtime.Storage[_storageKey] = _storedAddress;
        var service = CreateService(runtime, handler);
        await service.LoadAsync().ConfigureAwait(false);

        var result = await service.ConnectAsync(_apiAddress).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.Not.EqualTo(ApiResultStatus.Success));
            Assert.That(service.BaseAddress, Is.EqualTo(new Uri(_storedAddress)));
            Assert.That(runtime.Storage[_storageKey], Is.EqualTo(_storedAddress));
        }
    }

    [Test]
    public void ConnectAsync_WhenTheAddressNeverAnswers_GivesUpRatherThanWaiting()
    {
        using var handler = new DelayedHeadersHttpMessageHandler(TimeSpan.FromSeconds(30), "\"1.2.3\"");
        var service = CreateService(new(), handler);

        _ = Assert.CatchAsync<OperationCanceledException>(async () =>
            await service.ConnectAsync(_apiAddress).ConfigureAwait(false));

        Assert.That(service.BaseAddress, Is.EqualTo(_defaultAddress));
    }

    private static ApiAddressService CreateService(FakeJSRuntime runtime, HttpMessageHandler handler)
    {
        return new(
            new FakeWebAssemblyHostEnvironment(_hostAddress),
            new FakeHttpClientFactory(handler),
            new LocalStorageService(runtime));
    }
}
