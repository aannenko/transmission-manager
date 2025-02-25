using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal readonly record struct FindTorrentPageResponse(IReadOnlyList<Torrent> Torrents, string? NextPageAddress);
