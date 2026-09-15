using System.Net;
using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Tests.Extensions;

[Parallelizable(ParallelScope.All)]
internal sealed class HttpStatusCodeExtensionsTests
{
    [TestCase(HttpStatusCode.NotFound, ApiResultStatus.NotFound)]
    [TestCase(HttpStatusCode.Conflict, ApiResultStatus.Conflict)]
    [TestCase(HttpStatusCode.BadRequest, ApiResultStatus.Failed)]
    [TestCase(HttpStatusCode.FailedDependency, ApiResultStatus.Failed)]
    [TestCase(HttpStatusCode.OK, ApiResultStatus.Failed)]
    [TestCase(null, ApiResultStatus.Failed)]
    public void ToFailureStatus_WhenGivenAStatus_MapsItToTheOutcomeItReports(
        HttpStatusCode? statusCode,
        ApiResultStatus expected)
    {
        Assert.That(statusCode.ToFailureStatus(), Is.EqualTo(expected));
    }

    [TestCase((HttpStatusCode)199, false)]
    [TestCase(HttpStatusCode.OK, true)]
    [TestCase(HttpStatusCode.NoContent, true)]
    [TestCase((HttpStatusCode)299, true)]
    [TestCase(HttpStatusCode.MultipleChoices, false)]
    [TestCase(HttpStatusCode.InternalServerError, false)]
    public void IsSuccessCode_WhenGivenAStatus_ReportsWhetherItIsInTheSuccessRange(
        HttpStatusCode statusCode,
        bool expected)
    {
        Assert.That(statusCode.IsSuccessCode(), Is.EqualTo(expected));
    }
}
