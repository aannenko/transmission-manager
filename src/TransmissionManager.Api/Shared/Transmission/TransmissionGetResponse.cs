using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Shared.Transmission;

public readonly record struct TransmissionGetResponse(TransmissionTorrentGetResponseItem? Torrent, string? Error);
