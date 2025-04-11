using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Shared.Dto.Transmission;

[JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
public enum TransmissionAddResult
{
    Added,
    Duplicate
}
