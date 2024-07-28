using System.Text.Json.Serialization;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Transmission.Models;

namespace TransmissionManager.Api.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TorrentPostRequest))]
[JsonSerializable(typeof(TorrentPutRequest))]
[JsonSerializable(typeof(TransmissionTorrentGetRequest))]
[JsonSerializable(typeof(TransmissionTorrentGetResponse))]
[JsonSerializable(typeof(TransmissionTorrentAddRequest))]
[JsonSerializable(typeof(TransmissionTorrentAddResponse))]
[JsonSerializable(typeof(Torrent[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
