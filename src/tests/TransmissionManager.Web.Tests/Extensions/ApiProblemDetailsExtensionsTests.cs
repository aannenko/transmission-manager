using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Tests.Extensions;

[Parallelizable(ParallelScope.Self)]
internal sealed class ApiProblemDetailsExtensionsTests
{
    /// <remarks>
    /// A synthetic response: the API keys each message to one field or concept today, and reports a
    /// rule spanning the whole body under <c>Request</c>. This pins that nothing is dropped or
    /// merged should that ever change.
    /// </remarks>
    [Test]
    public void JoinErrorMessages_WhenOneMessageIsKeyedToSeveralFields_RepeatsItUnderEachOfThem()
    {
        const string message = "At least one field must be provided.";
        var problemDetails = new ApiProblemDetails(
            new()
            {
                ["DownloadDir"] = [message],
                ["MagnetRegexPattern"] = [message],
                ["JsonValueFormat"] = [message],
                ["Cron"] = [message],
            },
            null,
            null);

        Assert.That(
            problemDetails.JoinErrorMessages(),
            Is.EqualTo(
                $"DownloadDir: {message} MagnetRegexPattern: {message} "
                + $"JsonValueFormat: {message} Cron: {message}"));
    }

    /// <remarks>
    /// The source client says only that "the server responded", so without the key the reader cannot
    /// tell the torrent source from Transmission - the other dependency a request talks to.
    /// </remarks>
    [Test]
    public void JoinErrorMessages_WhenAMessageDoesNotSayWhatItIsAbout_NamesItsKey()
    {
        var problemDetails = new ApiProblemDetails(
            new() { ["torrentSource"] = ["The server responded with 502 BadGateway"] },
            null,
            null);

        Assert.That(
            problemDetails.JoinErrorMessages(),
            Is.EqualTo("torrentSource: The server responded with 502 BadGateway"));
    }

    [Test]
    public void JoinErrorMessages_WhenFieldsFailSeparately_NamesEachOfThem()
    {
        var problemDetails = new ApiProblemDetails(
            new()
            {
                ["JsonValueFormat"] = ["Invalid magnet format - it must contain '{0}' and no other braces."],
                ["Cron"] = ["Invalid or unsupported cron expression."],
            },
            null,
            null);

        Assert.That(
            problemDetails.JoinErrorMessages(),
            Is.EqualTo(
                "JsonValueFormat: Invalid magnet format - it must contain '{0}' and no other braces. "
                + "Cron: Invalid or unsupported cron expression."));
    }

    [Test]
    public void JoinErrorMessages_WhenOneKeyCarriesSeveralMessages_KeepsAllOfThem()
    {
        var problemDetails = new ApiProblemDetails(
            new() { ["MagnetRegexPattern"] = ["Invalid regex for magnet link search.", "Too long."] },
            null,
            null);

        Assert.That(
            problemDetails.JoinErrorMessages(),
            Is.EqualTo(
                "MagnetRegexPattern: Invalid regex for magnet link search. MagnetRegexPattern: Too long."));
    }

    [Test]
    public void JoinErrorMessages_WhenTheResponseCarriesNoMessages_ReturnsEmpty()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new ApiProblemDetails(null, null, null).JoinErrorMessages(), Is.Empty);
            Assert.That(
                new ApiProblemDetails(new() { ["torrent"] = null, ["id"] = [""] }, null, null).JoinErrorMessages(),
                Is.Empty);
        }
    }
}
