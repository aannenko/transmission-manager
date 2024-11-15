using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.RefreshTorrentById;

internal readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
