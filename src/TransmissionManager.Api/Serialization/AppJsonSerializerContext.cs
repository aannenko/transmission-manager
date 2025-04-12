using System.Text.Json.Serialization;
using TransmissionManager.Api.Common.Dto.AppInfo;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(AddTorrentRequest))]
[JsonSerializable(typeof(UpdateTorrentByIdRequest))]
[JsonSerializable(typeof(FindTorrentPageResponse))]
[JsonSerializable(typeof(AddTorrentResponse))]
[JsonSerializable(typeof(RefreshTorrentByIdResponse))]
[JsonSerializable(typeof(GetAppInfoResponse))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
