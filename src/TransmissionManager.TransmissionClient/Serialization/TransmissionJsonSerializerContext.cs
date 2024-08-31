﻿using System.Text.Json.Serialization;
using TransmissionManager.TransmissionClient.Dto;

namespace TransmissionManager.TransmissionClient.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TransmissionTorrentGetRequest))]
[JsonSerializable(typeof(TransmissionTorrentGetResponse))]
[JsonSerializable(typeof(TransmissionTorrentAddRequest))]
[JsonSerializable(typeof(TransmissionTorrentAddResponse))]
internal partial class TransmissionJsonSerializerContext : JsonSerializerContext
{
}
