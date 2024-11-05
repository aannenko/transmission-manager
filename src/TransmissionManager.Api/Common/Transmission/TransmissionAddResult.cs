using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Common.Transmission;

[JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
public enum TransmissionAddResult
{
    Added,
    Duplicate
}
