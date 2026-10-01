namespace TransmissionManager.Transmission.Dto;

/// <summary>Represents a Transmission <c>torrent-get</c> response.</summary>
public sealed class TransmissionTorrentGetResponse : ITransmissionResponse
{
    /// <summary>Gets Transmission's RPC result.</summary>
    /// <returns>The result text.</returns>
    public required string Result { get; init; }

    /// <summary>Gets the returned torrent arguments.</summary>
    /// <returns>The response arguments, or <see langword="null"/> when omitted.</returns>
    public TransmissionTorrentGetResponseArguments? Arguments { get; init; }

    /// <summary>Gets the optional request correlation tag.</summary>
    /// <returns>The correlation tag.</returns>
    public int? Tag { get; init; }
}

/// <summary>Contains torrents returned by <c>torrent-get</c>.</summary>
public sealed class TransmissionTorrentGetResponseArguments
{
    /// <summary>Gets the returned torrents.</summary>
    /// <returns>The torrent list, or <see langword="null"/> when omitted.</returns>
    public IReadOnlyList<TransmissionTorrentGetResponseItem>? Torrents { get; init; }
}

/// <summary>Represents fields returned for one torrent.</summary>
public sealed class TransmissionTorrentGetResponseItem
{
    /// <summary>Gets the torrent info hash.</summary>
    /// <returns>The info hash.</returns>
    public string? HashString { get; init; } // used instead of Id

    /// <summary>Gets the torrent name.</summary>
    /// <returns>The torrent name.</returns>
    public string? Name { get; init; }

    /// <summary>Gets the final size in bytes.</summary>
    /// <returns>The final size.</returns>
    public long? SizeWhenDone { get; init; }

    /// <summary>Gets the completed fraction.</summary>
    /// <returns>The completion fraction.</returns>
    public double? PercentDone { get; init; }

    /// <summary>Gets the download directory.</summary>
    /// <returns>The download directory.</returns>
    public string? DownloadDir { get; init; }
}
