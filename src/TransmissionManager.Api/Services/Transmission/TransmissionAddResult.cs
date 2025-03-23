using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Services.Transmission;

[JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
internal enum TransmissionAddResult
{
    Added,
    Duplicate
}
