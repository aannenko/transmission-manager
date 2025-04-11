using System.Text.Json.Serialization;
using TransmissionManager.Api.Shared.Dto.AppInfo.Get;
using TransmissionManager.Api.Shared.Dto.Torrents.Add;
using TransmissionManager.Api.Shared.Dto.Torrents.FindPage;
using TransmissionManager.Api.Shared.Dto.Torrents.RefreshById;
using TransmissionManager.Api.Shared.Dto.Torrents.UpdateById;
using TransmissionManager.Api.Shared.Dto.TransmissionInfo.Get;

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
