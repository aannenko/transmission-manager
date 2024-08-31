using System.Text.Json.Serialization;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Endpoints.Dto;

namespace TransmissionManager.Api.Endpoints.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TorrentPostRequest))]
[JsonSerializable(typeof(TorrentPatchRequest))]
[JsonSerializable(typeof(Torrent[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
