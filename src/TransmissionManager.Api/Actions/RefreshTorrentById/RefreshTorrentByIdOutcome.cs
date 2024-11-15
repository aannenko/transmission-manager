using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.RefreshTorrentById;

internal readonly record struct RefreshTorrentByIdOutcome(
    RefreshTorrentByIdResult Result,
    TransmissionAddResult? TransmissionResult,
    string? Error);
