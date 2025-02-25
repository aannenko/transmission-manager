namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal readonly record struct FindTorrentPageResponse(IReadOnlyList<Database.Models.Torrent> Torrents, string? NextPageAddress);
