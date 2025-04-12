using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Actions.Torrents;

internal readonly record struct AddTorrentOutcome(
    AddTorrentResult Result,
    long? Id,
    TransmissionAddResult? TransmissionResult,
    string? Error);
