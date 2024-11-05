using TransmissionManager.Api.Shared.Transmission;

namespace TransmissionManager.Api.RefreshTorrentById;

public readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
