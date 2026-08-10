namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal enum AddTorrentResult
{
    /// <summary>
    /// The torrent was added to both Transmission and the local catalog.
    /// </summary>
    Added,

    /// <summary>
    /// Another torrent already holds the same web page address or hash.
    /// </summary>
    /// <remarks>
    /// The magnet was still handed to Transmission before the local insert was rejected, so the
    /// response reports what Transmission did with it.
    /// </remarks>
    Exists,

    /// <summary>
    /// The request's source address or magnet regex yielded no magnet.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="DependencyFailed"/>: the source was never reached,
    /// or it answered and simply held no magnet at the selector, so retrying unchanged
    /// changes nothing. Reachable despite request validation, because a magnet regex can be shaped
    /// correctly yet fail to compile.
    /// </remarks>
    InvalidRequest,

    /// <summary>
    /// The torrent web page or Transmission could not be reached.
    /// </summary>
    /// <remarks>
    /// Nothing was added anywhere; the request itself is fine and may be retried as-is.
    /// </remarks>
    DependencyFailed
}
