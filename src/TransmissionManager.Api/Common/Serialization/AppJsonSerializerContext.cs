using System.Text.Json.Serialization;
using TransmissionManager.Api.AddOrUpdateTorrent.Request;
using TransmissionManager.Api.UpdateTorrentById.Request;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Common.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TorrentAddOrUpdateRequest))]
[JsonSerializable(typeof(TorrentPatchRequest))]
[JsonSerializable(typeof(Torrent[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
