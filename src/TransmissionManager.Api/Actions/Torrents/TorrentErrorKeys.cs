using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents;

/// <remarks>
/// A key names what the caller has to look at, under the name the caller already knows it by - a
/// body field as data annotation failures report it, or a route or query parameter as it appears in
/// the address. <see cref="Transmission"/> is the exception: nothing the caller sent is at fault.
/// </remarks>
internal static class TorrentErrorKeys
{
    public const string Id = "id";

    public const string Version = "version";

    /// <summary>
    /// Transmission refused the request, could not be reached, or does not hold the torrent the
    /// request is about.
    /// </summary>
    public const string Transmission = "transmission";

    /// <summary>
    /// The torrent's source as a whole - its address, its magnet pattern and its magnet format
    /// together.
    /// </summary>
    /// <remarks>
    /// A failed magnet search does not name which of the three is to blame, and mostly cannot: a
    /// pattern or a format that is malformed is refused before the source is ever read, so what
    /// reaches here is what only the message can explain - a page holding no magnet, a pattern
    /// matching nothing, a pointer addressing the wrong value.
    /// </remarks>
    public const string Source = "source";

    public const string SourceUri = nameof(AddTorrentRequest.SourceUri);
}
