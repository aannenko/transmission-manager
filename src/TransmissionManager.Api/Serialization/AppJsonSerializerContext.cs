using System.Text.Json.Serialization;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Transmission.Models;

namespace TransmissionManager.Api.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TorrentGetRequest))]
[JsonSerializable(typeof(TorrentGetResponse))]
[JsonSerializable(typeof(TorrentAddRequest))]
[JsonSerializable(typeof(TorrentAddResponse))]
[JsonSerializable(typeof(Torrent[]))]
[JsonSerializable(typeof(TorrentPostRequest))]
[JsonSerializable(typeof(TorrentPutRequest))]
[JsonSerializable(typeof(bool))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
