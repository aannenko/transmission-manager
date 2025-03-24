using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal readonly record struct FindTorrentPageResponse(
    Torrent[] Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
