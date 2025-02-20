using System.Text.Json.Serialization;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Transmission.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TransmissionTorrentGetRequest))]
[JsonSerializable(typeof(TransmissionTorrentGetResponse))]
[JsonSerializable(typeof(TransmissionTorrentAddRequest))]
[JsonSerializable(typeof(TransmissionTorrentAddResponse))]
[JsonSerializable(typeof(TransmissionTorrentRemoveRequest))]
[JsonSerializable(typeof(TransmissionTorrentRemoveResponse))]
internal sealed partial class TransmissionJsonSerializerContext : JsonSerializerContext;
