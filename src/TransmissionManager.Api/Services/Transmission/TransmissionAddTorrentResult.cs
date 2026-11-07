namespace TransmissionManager.Api.Services.Transmission;

internal enum TransmissionAddTorrentResult
{
    /// <summary>
    /// Transmission added the torrent.
    /// </summary>
    Added,

    /// <summary>
    /// Transmission already held the torrent.
    /// </summary>
    Duplicate,

    /// <summary>
    /// The request to Transmission failed, or Transmission reported an error.
    /// </summary>
    RequestFailed,

    /// <summary>
    /// Transmission reported success without naming the torrent as added or as a duplicate.
    /// </summary>
    UnexpectedResponse,
}
