namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Returns a torrent page and its navigation addresses.</summary>
/// <param name="Torrents">The torrents in the page.</param>
/// <param name="NextPageAddress">The next-page address, or <see langword="null"/> at the boundary.</param>
/// <param name="PreviousPageAddress">The previous-page address, or <see langword="null"/> at the boundary.</param>
/// <param name="Count">The total number of torrents matching the filters.</param>
public sealed record GetTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress,
    long Count);
