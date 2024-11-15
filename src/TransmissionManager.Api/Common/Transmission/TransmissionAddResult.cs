using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Common.Transmission;

[JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
internal enum TransmissionAddResult
{
    Added,
    Duplicate
}
