using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Actions.Torrents;

internal readonly record struct RefreshTorrentByIdOutcome(
    RefreshTorrentByIdResult Result,
    TorrentDto? TorrentDto,
    TransmissionAddResult? TransmissionResult,
    string? Error);
