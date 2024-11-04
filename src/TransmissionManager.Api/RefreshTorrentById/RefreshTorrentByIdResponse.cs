using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.RefreshTorrentById;

public readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
