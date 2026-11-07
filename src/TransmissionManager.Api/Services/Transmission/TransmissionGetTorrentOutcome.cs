using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Services.Transmission;

internal readonly record struct TransmissionGetTorrentOutcome(
    TransmissionGetTorrentResult Result,
    TransmissionTorrentGetResponseItem? Torrent,
    string? Error);
