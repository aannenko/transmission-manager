namespace TransmissionManager.Api.Services.Transmission;

internal enum TransmissionGetTorrentResult
{
    /// <summary>
    /// Transmission holds the torrent.
    /// </summary>
    Found,

    /// <summary>
    /// Transmission answered without the torrent.
    /// </summary>
    NotFound,

    /// <summary>
    /// The request to Transmission failed, or Transmission reported an error.
    /// </summary>
    RequestFailed,
}
