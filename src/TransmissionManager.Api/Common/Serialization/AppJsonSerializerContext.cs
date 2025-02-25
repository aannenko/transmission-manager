using System.Text.Json.Serialization;
using TransmissionManager.Api.Actions.LocalTime.Get;
using TransmissionManager.Api.Actions.Torrents.Add;
using TransmissionManager.Api.Actions.Torrents.FindPage;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Actions.Torrents.UpdateById;

namespace TransmissionManager.Api.Common.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AddTorrentRequest))]
[JsonSerializable(typeof(UpdateTorrentByIdRequest))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(FindTorrentPageResponse))]
[JsonSerializable(typeof(AddTorrentResponse))]
[JsonSerializable(typeof(RefreshTorrentByIdResponse))]
[JsonSerializable(typeof(GetLocalTimeResponse))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
