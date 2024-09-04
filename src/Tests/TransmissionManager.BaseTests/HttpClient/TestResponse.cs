using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed record TestResponse(
    HttpStatusCode StatusCode,
    Dictionary<string, string>? Headers = null,
    string? Content = null);
