using System.Text.Json.Serialization;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Endpoints.Dto;

namespace TransmissionManager.Api.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TorrentPostRequest))]
[JsonSerializable(typeof(TorrentPutRequest))]
[JsonSerializable(typeof(Torrent[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
