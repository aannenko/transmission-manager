namespace TransmissionManager.Api.Common.Constants;

/// <summary>
/// The keys this API reports errors under.
/// </summary>
public static class ProblemDetailsKeys
{
    #region Extension members

    public static readonly string CurrentVersion = "currentVersion";

    public static readonly string TransmissionResult = "transmissionResult";

    /// <summary>
    /// Carries the dictionary of everything that is wrong, keyed by what is at fault.
    /// </summary>
    /// <remarks>
    /// Each value is an array of messages. Every key in the regions below goes inside it.
    /// </remarks>
    public static readonly string Errors = "errors";

    #endregion

    #region Request parameters

    public static readonly string Id = "id";

    public static readonly string Version = "version";

    #endregion

    #region Request processing

    /// <summary>
    /// Blames the request as a whole for breaking a rule that no single field breaks on its own.
    /// </summary>
    /// <remarks>
    /// Pascal-case because it arrives among the framework's own body-field keys. The message says
    /// which part of the request it means, so the key does not repeat it.
    /// </remarks>
    public static readonly string Request = "Request";

    /// <summary>
    /// Blames Transmission for refusing the request, being unreachable, or not holding the torrent.
    /// </summary>
    public static readonly string Transmission = "transmission";

    /// <summary>
    /// Blames the torrent for a duplicate source URI or hash of another existing torrent.
    /// </summary>
    public static readonly string Torrent = "torrent";

    /// <summary>
    /// Blames the torrent's source for not yielding a magnet link.
    /// </summary>
    /// <remarks>
    /// Covers the source's address, magnet pattern and magnet format together, because a failure
    /// rarely points at just one of them.
    /// </remarks>
    public static readonly string TorrentSource = "torrentSource";

    #endregion
}
