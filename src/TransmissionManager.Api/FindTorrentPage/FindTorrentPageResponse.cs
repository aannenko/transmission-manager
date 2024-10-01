using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.FindTorrentPage;

public readonly record struct FindTorrentPageResponse(Torrent[] Torrents, string? NextPageAddress);
