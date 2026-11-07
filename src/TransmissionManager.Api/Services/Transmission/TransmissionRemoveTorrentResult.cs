namespace TransmissionManager.Api.Services.Transmission;

internal enum TransmissionRemoveTorrentResult
{
    /// <summary>
    /// Transmission reported the removal as successful.
    /// </summary>
    Removed,

    /// <summary>
    /// The request to Transmission failed, or Transmission reported an error.
    /// </summary>
    RequestFailed,
}
