using System.Text.Json.Serialization;
using TransmissionManager.Api.Transmission.Dto;

namespace TransmissionManager.Api.Transmission.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TransmissionTorrentGetRequest))]
[JsonSerializable(typeof(TransmissionTorrentGetResponse))]
[JsonSerializable(typeof(TransmissionTorrentAddRequest))]
[JsonSerializable(typeof(TransmissionTorrentAddResponse))]
public partial class TransmissionJsonSerializerContext : JsonSerializerContext
{
}
