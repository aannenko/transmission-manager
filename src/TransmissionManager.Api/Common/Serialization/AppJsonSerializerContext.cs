using System.Text.Json.Serialization;
using TransmissionManager.Api.AddTorrent;
using TransmissionManager.Api.FindTorrentPage;
using TransmissionManager.Api.RefreshTorrentById;
using TransmissionManager.Api.UpdateTorrentById;

namespace TransmissionManager.Api.Common.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AddTorrentRequest))]
[JsonSerializable(typeof(UpdateTorrentByIdRequest))]
[JsonSerializable(typeof(FindTorrentPageResponse))]
[JsonSerializable(typeof(AddTorrentResponse))]
[JsonSerializable(typeof(RefreshTorrentByIdResponse))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
