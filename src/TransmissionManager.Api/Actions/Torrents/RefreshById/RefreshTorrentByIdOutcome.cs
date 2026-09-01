using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

/// <param name="Warning">
/// Something that went wrong without failing the refresh, or <see langword="null"/>. Set only on the
/// success path; a refresh that failed reports why through <paramref name="Errors"/>.
/// </param>
internal readonly record struct RefreshTorrentByIdOutcome(
    RefreshTorrentByIdResult Result,
    TorrentDto? TorrentDto,
    TransmissionAddResult? TransmissionResult,
    string? Warning,
    KeyValuePair<string, string[]>[] Errors,
    long? CurrentVersion = null);
