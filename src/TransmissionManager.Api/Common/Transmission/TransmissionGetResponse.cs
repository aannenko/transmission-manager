using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Common.Transmission;

public readonly record struct TransmissionGetResponse(TransmissionTorrentGetResponseItem? Torrent, string? Error);
