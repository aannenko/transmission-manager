namespace TransmissionManager.Api.Common.Dto.Torrents;

public readonly record struct FindTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
