using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Common.Transmission;

internal readonly record struct TransmissionGetResponse(TransmissionTorrentGetResponseItem? Torrent, string? Error);
