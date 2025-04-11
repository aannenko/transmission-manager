using TransmissionManager.Api.Shared.Dto.Transmission;

namespace TransmissionManager.Api.Shared.Dto.Torrents.RefreshById;

public readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
