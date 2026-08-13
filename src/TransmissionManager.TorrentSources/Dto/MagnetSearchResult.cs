namespace TransmissionManager.TorrentSources.Dto;

/// <summary>
/// The outcome of a search for a magnet link.
/// </summary>
/// <remarks>
/// Expected failures are reported through these members rather than thrown.
/// </remarks>
public enum MagnetSearchResult
{
    /// <summary>
    /// The magnet link was found.
    /// </summary>
    Found,

    /// <summary>
    /// The source was read successfully, but holds no magnet link at the selector.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="RetrievalFailed"/>, repeating the search changes nothing until the source
    /// itself changes.
    /// </remarks>
    NotFound,

    /// <summary>
    /// The source could not be read: a connection failure, timeout, open circuit or an
    /// unsuccessful status.
    /// </summary>
    /// <remarks>
    /// Covers failures raised while streaming the body, not only while awaiting the response.
    /// Anti-bot challenges land here, or in <see cref="NotFound"/> when served with a successful
    /// status: recognising one vendor's challenge would imply recognising every vendor's, so none
    /// are singled out.
    /// </remarks>
    RetrievalFailed,

    /// <summary>
    /// The source address is not an absolute <c>http</c> or <c>https</c> address.
    /// </summary>
    /// <remarks>
    /// Reported without requesting anything.
    /// </remarks>
    InvalidSource,

    /// <summary>
    /// The selector that extracts the magnet link from the source is malformed.
    /// </summary>
    /// <remarks>
    /// Matching the required shape does not prove a magnet regex compiles - <c>magnet:\?xt=(</c>
    /// passes the shape check and then throws - so a selector can fail here despite validation.
    /// </remarks>
    InvalidSelector,
}
