using System.Text.Json.Serialization;
using TransmissionManager.Api.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Serialization;

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
