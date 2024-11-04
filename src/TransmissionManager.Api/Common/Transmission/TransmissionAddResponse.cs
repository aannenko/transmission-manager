using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Common.Transmission;

public readonly record struct TransmissionAddResponse(
    TransmissionAddResult? Result,
    TransmissionTorrentAddResponseItem? Torrent,
    string? Error);
