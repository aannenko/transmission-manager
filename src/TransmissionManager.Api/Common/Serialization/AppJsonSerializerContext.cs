﻿using System.Text.Json.Serialization;
using TransmissionManager.Api.Actions.AddTorrent;
using TransmissionManager.Api.Actions.FindTorrentPage;
using TransmissionManager.Api.Actions.RefreshTorrentById;
using TransmissionManager.Api.Actions.UpdateTorrentById;

namespace TransmissionManager.Api.Common.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AddTorrentRequest))]
[JsonSerializable(typeof(UpdateTorrentByIdRequest))]
[JsonSerializable(typeof(FindTorrentPageResponse))]
[JsonSerializable(typeof(AddTorrentResponse))]
[JsonSerializable(typeof(RefreshTorrentByIdResponse))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
