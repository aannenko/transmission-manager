using System.Text.Json.Serialization;

namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>
/// How a torrent's magnet link is extracted from its source.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TorrentSourceKind>))]
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
