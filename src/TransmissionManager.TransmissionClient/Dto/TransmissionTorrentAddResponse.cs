using System.Text.Json.Serialization;

namespace TransmissionManager.TransmissionClient.Dto;

public sealed class TransmissionTorrentAddResponse : ITransmissionResponse
{
    public required string Result { get; init; }

    public TransmissionTorrentAddResponseArguments? Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentAddResponseArguments
{
    [JsonPropertyName("torrent-added")]
    public TransmissionTorrentAddResponseItem? TorrentAdded { get; init; }

    [JsonPropertyName("torrent-duplicate")]
    public TransmissionTorrentAddResponseItem? TorrentDuplicate { get; init; }
}

public sealed class TransmissionTorrentAddResponseItem
{
    public required string HashString { get; init; }

    public required string Name { get; init; }
}
