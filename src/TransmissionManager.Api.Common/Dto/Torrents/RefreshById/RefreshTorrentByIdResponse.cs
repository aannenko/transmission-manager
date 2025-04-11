using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents.RefreshById;

public readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
