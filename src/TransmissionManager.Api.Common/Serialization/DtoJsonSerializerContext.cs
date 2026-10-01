using System.Text.Json.Serialization;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Common.Serialization;

/// <summary>Provides JSON serialization metadata for API data transfer objects.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AddTorrentRequest))]
[JsonSerializable(typeof(UpdateTorrentByIdRequest))]
[JsonSerializable(typeof(GetTorrentPageResponse))]
[JsonSerializable(typeof(AddTorrentResponse))]
[JsonSerializable(typeof(RefreshTorrentByIdResponse))]
[JsonSerializable(typeof(Version))]
public sealed partial class DtoJsonSerializerContext : JsonSerializerContext;
