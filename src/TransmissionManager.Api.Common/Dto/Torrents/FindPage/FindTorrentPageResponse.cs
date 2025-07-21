namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed record FindTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
