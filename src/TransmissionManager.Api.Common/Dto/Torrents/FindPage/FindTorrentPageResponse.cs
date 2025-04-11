namespace TransmissionManager.Api.Common.Dto.Torrents.FindPage;

public readonly record struct FindTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
