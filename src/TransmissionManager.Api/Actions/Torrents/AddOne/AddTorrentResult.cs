namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal enum AddTorrentResult
{
    /// <summary>
    /// The torrent is present in Transmission and was added to the local catalog.
    /// </summary>
    Added,

    /// <summary>
    /// Another torrent already holds the same source URI or hash.
    /// </summary>
    /// <remarks>
    /// The magnet was still handed to Transmission before the local insert was rejected, so the
    /// response reports what Transmission did with it.
    /// </remarks>
    Exists,

    /// <summary>
    /// The request's torrent source configuration did not yield a magnet.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="DependencyFailed"/>: the source was never reached,
    /// or it answered and simply held no magnet at the selector, so retrying unchanged
    /// changes nothing. Reachable despite request validation, because a magnet regex can be shaped
    /// correctly yet fail to compile.
    /// </remarks>
    InvalidRequest,

    /// <summary>
    /// The torrent source could not be retrieved, or Transmission could not accept the magnet.
    /// </summary>
    /// <remarks>
    /// Nothing was added anywhere; the request itself is fine and may be retried as-is.
    /// </remarks>
    DependencyFailed
}
