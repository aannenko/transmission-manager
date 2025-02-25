using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.Torrents.Add;

internal readonly record struct AddTorrentOutcome(
    AddTorrentResult Result,
    long? Id,
    TransmissionAddResult? TransmissionResult,
    string? Error);
