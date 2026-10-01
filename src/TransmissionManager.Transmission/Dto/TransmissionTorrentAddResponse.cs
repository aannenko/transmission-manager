using System.Text.Json.Serialization;

namespace TransmissionManager.Transmission.Dto;

/// <summary>Represents a Transmission <c>torrent-add</c> response.</summary>
public sealed class TransmissionTorrentAddResponse : ITransmissionResponse
{
    /// <summary>Gets Transmission's RPC result.</summary>
    /// <returns>The result text.</returns>
    public required string Result { get; init; }

    /// <summary>Gets the add-result arguments.</summary>
    /// <returns>The response arguments, or <see langword="null"/> when omitted.</returns>
    public TransmissionTorrentAddResponseArguments? Arguments { get; init; }

    /// <summary>Gets the optional request correlation tag.</summary>
    /// <returns>The correlation tag.</returns>
    public int? Tag { get; init; }
}

/// <summary>Contains the torrent accepted by a <c>torrent-add</c> request.</summary>
public sealed class TransmissionTorrentAddResponseArguments
{
    /// <summary>Gets the newly added torrent.</summary>
    /// <returns>The added torrent, or <see langword="null"/> when no torrent was added.</returns>
    [JsonPropertyName("torrent-added")]
    public TransmissionTorrentAddResponseItem? TorrentAdded { get; init; }

    /// <summary>Gets the torrent Transmission already held.</summary>
    /// <returns>The duplicate torrent, or <see langword="null"/> when no duplicate was found.</returns>
    [JsonPropertyName("torrent-duplicate")]
    public TransmissionTorrentAddResponseItem? TorrentDuplicate { get; init; }
}

/// <summary>Identifies a torrent returned by <c>torrent-add</c>.</summary>
public sealed class TransmissionTorrentAddResponseItem
{
    /// <summary>Gets the torrent info hash.</summary>
    /// <returns>The info hash.</returns>
    public required string HashString { get; init; }

    /// <summary>Gets the torrent name.</summary>
    /// <returns>The torrent name.</returns>
    public required string Name { get; init; }
}
