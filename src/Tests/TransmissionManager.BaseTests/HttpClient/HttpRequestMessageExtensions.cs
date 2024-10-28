using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransmissionManager.BaseTests.HttpClient;

internal static class HttpRequestMessageExtensions
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<TestRequest> ToTestRequestAsync(this HttpRequestMessage request)
    {
        return new(
            request.Method,
            request.RequestUri,
            request.Headers.ToDictionary(static pair => pair.Key, static pair => pair.Value.Single()),
            request.Content switch
            {
                null => null,
                JsonContent content => JsonSerializer.Serialize(content.Value, content.ObjectType, _serializerOptions),
                _ => await request.Content.ReadAsStringAsync()
            });
    }
}
