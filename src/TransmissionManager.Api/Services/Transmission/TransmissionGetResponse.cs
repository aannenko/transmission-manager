using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Services.Transmission;

internal readonly record struct TransmissionGetResponse(TransmissionTorrentGetResponseItem? Torrent, string? Error);
