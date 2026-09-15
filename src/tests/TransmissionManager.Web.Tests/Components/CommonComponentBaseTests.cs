using System.Net;
using TransmissionManager.Web.Components;
using TransmissionManager.Web.Dto;

namespace TransmissionManager.Web.Tests.Components;

[Parallelizable(ParallelScope.Self)]
internal sealed class CommonComponentBaseTests
{
    private const string _busyMessage = "Working...";

    [Test]
    public async Task CallNetworkService_WhenTheCallSucceeds_ReturnsItsResultAndStopsBeingBusy()
    {
        var component = new TestComponent();

        var result = await component
            .RunAsync(static _ => Task.FromResult(ApiResult<string>.Success(HttpStatusCode.OK, "1.2.3")))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Success));
            Assert.That(result.Value, Is.EqualTo("1.2.3"));
            Assert.That(component.IsBusyNow, Is.False);
        }
    }

    [Test]
    public async Task CallNetworkService_WhenTheApiRefusesTheCall_ShowsWhatItSaid()
    {
        var component = new TestComponent();
        var problemDetails = new ApiProblemDetails(new() { ["torrent"] = ["Already exists."] }, null, null);

        var result = await component
            .RunAsync(_ => Task.FromResult(ApiResult<string>.Failure(HttpStatusCode.Conflict, problemDetails)))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Conflict));
            Assert.That(component.CurrentMessage, Is.EqualTo("torrent: Already exists."));
            Assert.That(component.IsBusyNow, Is.False);
        }
    }

    [Test]
    public async Task CallNetworkService_WhenTheCallIsCanceled_ReportsItAndAnswersWithAFailure()
    {
        var component = new TestComponent();

        var result = await component
            .RunAsync(static _ => Task.FromException<ApiResult<string>>(new OperationCanceledException()))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.Value, Is.Null);
            Assert.That(component.CurrentMessage, Is.EqualTo(TestComponent.CanceledMessage));
            Assert.That(component.IsBusyNow, Is.False);
        }
    }

    /// <remarks>
    /// The status code is what tells the two apart: a request that never reached a server carries
    /// none, while one the server refused outright carries the status it refused with.
    /// </remarks>
    [TestCase(null, TestComponent.DisconnectedMessage)]
    [TestCase(HttpStatusCode.InternalServerError, TestComponent.GenericErrorMessage)]
    public async Task CallNetworkService_WhenTheRequestThrows_ReportsItAccordingToItsStatus(
        HttpStatusCode? statusCode,
        string expectedMessage)
    {
        var component = new TestComponent();
        var exception = new HttpRequestException("boom", null, statusCode);

        var result = await component
            .RunAsync(_ => Task.FromException<ApiResult<string>>(exception))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(component.CurrentMessage, Is.EqualTo(expectedMessage));
            Assert.That(component.IsBusyNow, Is.False);
        }
    }

    /// <remarks>
    /// The two overloads carry the same body, so this guards them drifting apart.
    /// </remarks>
    [Test]
    public async Task CallNetworkService_OnTheOverloadWithoutContent_ReportsFailuresTheSameWay()
    {
        var component = new TestComponent();
        var exception = new HttpRequestException("boom", null, null);

        var result = await component
            .RunWithoutContentAsync(_ => Task.FromException<ApiResult>(exception))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(component.CurrentMessage, Is.EqualTo(TestComponent.DisconnectedMessage));
            Assert.That(component.IsBusyNow, Is.False);
        }
    }

    [TestCase(null, "The request could not be completed.")]
    [TestCase(
        HttpStatusCode.OK,
        "The address answered with status 200 (OK), but not with data this application understands.")]
    [TestCase(HttpStatusCode.BadGateway, "The request failed with status 502 (BadGateway).")]
    public void GetProblemDetailsMessage_WhenTheRefusalCarriesNoMessage_FallsBackToTheStatus(
        HttpStatusCode? statusCode,
        string expected)
    {
        var component = new TestComponent();

        Assert.That(component.DescribeProblem(null, statusCode), Is.EqualTo(expected));
    }

    [Test]
    public void GetProblemDetailsMessage_WhenTheRefusalCarriesAMessage_PrefersItOverTheStatus()
    {
        var component = new TestComponent();
        var problemDetails = new ApiProblemDetails(new() { ["cron"] = ["Invalid cron expression."] }, null, null);

        Assert.That(
            component.DescribeProblem(problemDetails, HttpStatusCode.BadRequest),
            Is.EqualTo("cron: Invalid cron expression."));
    }

    private sealed class TestComponent : CommonComponentBase
    {
        public const string CanceledMessage = "canceled";
        public const string DisconnectedMessage = "disconnected";
        public const string GenericErrorMessage = "generic";

        public bool IsBusyNow => IsBusy;

        public string CurrentMessage => Message;

        public Task<ApiResult<string>> RunAsync(Func<int, Task<ApiResult<string>>> func) =>
            CallNetworkService(0, func);

        public Task<ApiResult> RunWithoutContentAsync(Func<int, Task<ApiResult>> func) =>
            CallNetworkService(0, func);

        public string DescribeProblem(ApiProblemDetails? problemDetails, HttpStatusCode? statusCode) =>
            GetProblemDetailsMessage(problemDetails, statusCode);

        private protected override string BusyMessage => _busyMessage;

        private protected override string GetOperationCanceledMessage(OperationCanceledException exception) =>
            CanceledMessage;

        private protected override string GetDisconnectedMessage(HttpRequestException exception) =>
            DisconnectedMessage;

        private protected override string GetGenericErrorMessage(HttpRequestException exception) =>
            GenericErrorMessage;
    }
}
