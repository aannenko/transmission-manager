namespace TransmissionManager.Database.Dto;

/// <summary>
/// How a torrent's magnet link is extracted from its source.
/// </summary>
/// <remarks>
/// Persisted as an <c>int</c>, so the numeric values are a storage contract - renaming a member is
/// safe, reassigning its value is not. <see cref="WebPage"/> is <c>0</c> because that is the value
/// rows written before this property existed carry.
/// </remarks>
public enum TorrentSourceKind
{
    /// <summary>
    /// The source is an HTML page scanned with a regular expression.
    /// </summary>
    WebPage = 0,

    /// <summary>
    /// The source is a JSON document addressed by an RFC 6901 JSON Pointer carried in the
    /// fragment of the source URI.
    /// </summary>
    JsonPointer = 1,
}
