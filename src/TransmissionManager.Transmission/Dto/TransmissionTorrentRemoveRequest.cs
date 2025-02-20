using System.Text.Json.Serialization;

namespace TransmissionManager.Transmission.Dto;

public sealed class TransmissionTorrentRemoveRequest
{
#pragma warning disable CA1822 // Mark members as static - should not be static for System.Text.Json.JsonSerializer to serialize it
    public string Method => "torrent-remove";
#pragma warning restore CA1822 // Mark members as static

    public required TransmissionTorrentRemoveRequestArguments Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentRemoveRequestArguments
{
    [JsonPropertyName("ids")]
    public IReadOnlyList<string>? HashStrings { get; init; }

    [JsonPropertyName("delete-local-data")]
    public bool DeleteLocalData { get; init; }
}
