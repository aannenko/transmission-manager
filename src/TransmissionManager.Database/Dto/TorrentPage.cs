using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Dto;

/// <summary>
/// The result of <see cref="Services.TorrentService.GetPageAsync{T}"/>.
/// </summary>
/// <param name="Torrents">The page rows in canonical order.</param>
/// <param name="HasMore">
/// <c>true</c> iff at least one additional row exists in the fetched direction beyond
/// <paramref name="Torrents"/>. The DB layer does not interpret direction; callers map this
/// onto <c>HasNext</c> / <c>HasPrevious</c> per their own request semantics.
/// </param>
public readonly record struct TorrentPage(IReadOnlyList<Torrent> Torrents, bool HasMore);
