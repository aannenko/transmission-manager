using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

public readonly record struct FindTorrentPageResponse(IReadOnlyList<Torrent> Torrents, string? NextPageAddress);
