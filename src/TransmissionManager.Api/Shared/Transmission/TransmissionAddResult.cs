using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Shared.Transmission;

[JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
public enum TransmissionAddResult
{
    Added,
    Duplicate
}
