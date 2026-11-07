using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Services.Transmission;

internal readonly record struct TransmissionAddTorrentOutcome(
    TransmissionAddTorrentResult Result,
    TransmissionTorrentAddResponseItem? Torrent,
    string? Error);
