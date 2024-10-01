using static TransmissionManager.Api.Common.Services.TransmissionClientWrapper;

namespace TransmissionManager.Api.RefreshTorrentById;

public readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
