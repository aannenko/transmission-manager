using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Transmission.Models;

public sealed class TransmissionTorrentAddRequest
{
    public string Method { get; } = "torrent-add";

    public required TransmissionTorrentAddRequestArguments Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentAddRequestArguments
{
    public required string Filename { get; init; } // magnet uri

    [JsonPropertyName("download-dir")]
    public string? DownloadDir { get; init; }
}
