using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed record TestResponse(
    HttpStatusCode StatusCode,
    IReadOnlyDictionary<string, string>? Headers = null,
    string? Content = null);
