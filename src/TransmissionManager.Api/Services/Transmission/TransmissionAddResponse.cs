using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Services.Transmission;

internal readonly record struct TransmissionAddResponse(
    TransmissionAddResult? Result,
    TransmissionTorrentAddResponseItem? Torrent,
    string? Error);
