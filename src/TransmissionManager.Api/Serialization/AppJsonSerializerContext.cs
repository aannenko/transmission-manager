using System.Text.Json.Serialization;
using TransmissionManager.Api.Common.Dto.AppInfo;
using TransmissionManager.Api.Common.Dto.Torrents.Add;
using TransmissionManager.Api.Common.Dto.Torrents.FindPage;
using TransmissionManager.Api.Common.Dto.Torrents.RefreshById;
using TransmissionManager.Api.Common.Dto.Torrents.UpdateById;
using TransmissionManager.Api.Common.Dto.TransmissionInfo;

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
[JsonSerializable(typeof(GetTransmissionInfoResponse))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
