using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Transmission.Models;

public sealed class TorrentAddResponse : ITransmissionResponse
{
    public required string Result { get; init; }

    public TorrentAddResponseArguments? Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TorrentAddResponseArguments
{
    [JsonPropertyName("torrent-added")]
    public TorrentAddResponseItem? TorrentAdded { get; init; }

    [JsonPropertyName("torrent-duplicate")]
    public TorrentAddResponseItem? TorrentDuplicate { get; init; }
}

public sealed class TorrentAddResponseItem
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public required string HashString { get; init; }
}
