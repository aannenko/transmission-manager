namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed record GetTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
