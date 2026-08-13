using System.ComponentModel.DataAnnotations;
using TransmissionManager.TorrentSources.Options;

namespace TransmissionManager.TorrentSources.JsonPointer;

public sealed class TorrentJsonPointerClientOptions : TorrentSourcesOptions
{
    /// <summary>
    /// The buffer a JSON document is read through, and so the largest single token it may hold - a
    /// value of up to three bytes less, once its quotes and closing delimiter are counted.
    /// </summary>
    /// <remarks>
    /// This is the whole memory one search can occupy, which is what bounds a hostile or careless
    /// source: a document holding a larger token is rejected rather than buffered, even when the
    /// pointer does not address that token, because the reader cannot step over a value it cannot
    /// hold. It also bounds the length of a JSON Pointer segment, since a segment too long to hold
    /// could never be compared against a member name.
    /// <para>
    /// The shipped value, 4096, suits what torrent sources return - a magnet link or a torrent name
    /// runs to a few hundred characters - and only a source packing something much larger into the
    /// same document needs it raised; a document of nothing but hashes can take it lower.
    /// </para>
    /// </remarks>
    [Range(1024, 65536)]
    public required int MaxJsonTokenBytes { get; set; }
}
