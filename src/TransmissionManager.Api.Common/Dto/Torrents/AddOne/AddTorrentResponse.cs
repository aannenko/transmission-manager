using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Reports the cataloged torrent and how Transmission handled its add request.</summary>
/// <param name="TorrentDto">The persisted torrent.</param>
/// <param name="TransmissionResult">Whether Transmission added or already held the torrent.</param>
public sealed record AddTorrentResponse(
    TorrentDto TorrentDto,
    TransmissionAddResult TransmissionResult);
