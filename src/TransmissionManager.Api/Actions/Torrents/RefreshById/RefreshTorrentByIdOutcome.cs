using TransmissionManager.Api.Services.Transmission;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal readonly record struct RefreshTorrentByIdOutcome(
    RefreshTorrentByIdResult Result,
    TransmissionAddResult? TransmissionResult,
    string? Error);
