using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.AddTorrent;

public readonly record struct AddTorrentOutcome(
    AddTorrentResult Result,
    long? Id,
    TransmissionAddResult? TransmissionResult,
    string? Error);
