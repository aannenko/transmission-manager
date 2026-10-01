using System.Text.Json.Serialization;
using TransmissionManager.Transmission.Serialization;

namespace TransmissionManager.Transmission.Dto;

public sealed class TransmissionTorrentGetRequest
{
#pragma warning disable CA1822 // Mark members as static - should not be static for System.Text.Json.JsonSerializer to serialize it
    public string Method => "torrent-get";
#pragma warning restore CA1822 // Mark members as static

    public required TransmissionTorrentGetRequestArguments Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentGetRequestArguments
{
    [JsonPropertyName("ids")]
    public IReadOnlyList<string>? HashStrings { get; init; }

    public required IReadOnlyList<TransmissionTorrentGetRequestFields> Fields { get; init; }
}

/// <summary>Defines fields requested from Transmission's <c>torrent-get</c> method.</summary>
[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<TransmissionTorrentGetRequestFields>))]
public enum TransmissionTorrentGetRequestFields
{
    /// <summary>Requests the torrent info hash.</summary>
    HashString, // used instead of Id
    /// <summary>Requests the torrent name.</summary>
    Name,
    /// <summary>Requests the final size in bytes.</summary>
    SizeWhenDone,
    /// <summary>Requests the completed fraction.</summary>
    PercentDone,
    /// <summary>Requests the download directory.</summary>
    DownloadDir,
}
