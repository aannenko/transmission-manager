using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.RefreshTorrentById;

public readonly record struct RefreshTorrentByIdOutcome(
    RefreshTorrentByIdResult Result,
    TransmissionAddResult? TransmissionResult,
    string? Error);
