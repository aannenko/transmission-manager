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
    public required IReadOnlyList<TransmissionTorrentGetRequestFields> Fields { get; init; }

    [JsonPropertyName("ids")]
    public IReadOnlyList<string>? HashStrings { get; init; }
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<TransmissionTorrentGetRequestFields>))]
public enum TransmissionTorrentGetRequestFields
{
    HashString, // used instead of Id
    Name,
    SizeWhenDone,
    PercentDone,
    DownloadDir,
}
