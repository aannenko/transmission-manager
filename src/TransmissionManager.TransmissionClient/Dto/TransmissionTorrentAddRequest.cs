using System.Text.Json.Serialization;

namespace TransmissionManager.TransmissionClient.Dto;

public sealed class TransmissionTorrentAddRequest
{
#pragma warning disable CA1822 // Mark members as static
    // should not be static for System.Text.Json.JsonSerializer to serialize it
    public string Method => "torrent-add";
#pragma warning restore CA1822 // Mark members as static

    public required TransmissionTorrentAddRequestArguments Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentAddRequestArguments
{
    public required string Filename { get; init; } // magnet uri or base64-encoded torrent file contents

    [JsonPropertyName("download-dir")]
    public string? DownloadDir { get; init; }
}
