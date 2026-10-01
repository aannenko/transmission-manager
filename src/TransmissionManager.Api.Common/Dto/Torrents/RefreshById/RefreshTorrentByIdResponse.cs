using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Reports a refreshed torrent and its Transmission result.</summary>
/// <param name="TorrentDto">The refreshed torrent.</param>
/// <param name="TransmissionResult">Whether Transmission added or already held the refreshed torrent.</param>
/// <param name="Message">An optional result message.</param>
public sealed record RefreshTorrentByIdResponse(
    TorrentDto TorrentDto,
    TransmissionAddResult TransmissionResult,
    string? Message = null);
