using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Dto;

/// <param name="Result">
/// <see cref="TorrentMutationResult.Success"/> or <see cref="TorrentMutationResult.NotUnique"/>.
/// The remaining <see cref="TorrentMutationResult"/> members cannot occur on an insert.
/// </param>
/// <param name="Torrent">The inserted entity, or <see langword="null"/> when <paramref name="Result"/>
/// is not <see cref="TorrentMutationResult.Success"/>.</param>
public readonly record struct TorrentAddOutcome(TorrentMutationResult Result, Torrent? Torrent);
