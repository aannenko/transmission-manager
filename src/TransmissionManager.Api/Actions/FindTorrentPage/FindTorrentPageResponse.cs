using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

internal readonly record struct FindTorrentPageResponse(IReadOnlyList<Torrent> Torrents, string? NextPageAddress);
