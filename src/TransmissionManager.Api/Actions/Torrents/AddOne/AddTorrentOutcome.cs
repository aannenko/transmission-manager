using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Actions.Torrents;

internal readonly record struct AddTorrentOutcome(
    AddTorrentResult Result,
    TorrentDto? TorrentDto,
    TransmissionAddResult? TransmissionResult,
    string? Error);
