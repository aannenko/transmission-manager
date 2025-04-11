namespace TransmissionManager.Api.Shared.Dto.Torrents.FindPage;

public readonly record struct FindTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
