using TransmissionManager.Api.Shared.Transmission;

namespace TransmissionManager.Api.Actions.RefreshTorrentById;

public readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
