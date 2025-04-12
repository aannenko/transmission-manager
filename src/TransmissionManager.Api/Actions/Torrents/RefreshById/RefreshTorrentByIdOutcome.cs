using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Actions.Torrents;

internal readonly record struct RefreshTorrentByIdOutcome(
    RefreshTorrentByIdResult Result,
    TransmissionAddResult? TransmissionResult,
    string? Error);
