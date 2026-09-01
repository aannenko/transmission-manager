using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.Torrents.RefreshById.RefreshTorrentByIdResult;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal sealed class RefreshTorrentByIdHandler(
    IServiceProvider serviceProvider,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    BackgroundTorrentUpdateService backgroundUpdateService)
    : IRefreshTorrentByIdHandler
{
    public async Task<RefreshTorrentByIdOutcome> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken)
    {
        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return OnNotFoundLocally();

        var (_, transmissionGetError) = await transmissionService
            .GetTorrentAsync(torrent.HashString, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetError is not null)
            return OnNotFoundInTransmission(transmissionGetError);

        var (searchResult, magnetUri, getMagnetError) = await serviceProvider
            .FindMagnetUriAsync(
                new(torrent.SourceUri),
                torrent.SourceKind,
                torrent.MagnetRegexPattern,
                torrent.JsonValueFormat,
                cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
        {
            return searchResult.IsUnprocessableSource()
                ? OnInvalidConfiguration(getMagnetError!)
                : OnDependencyFailed(TorrentErrorKeys.Source, null, getMagnetError!);
        }

        var (transmissionAddResult, transmissionAddTorrent, transmissionAddError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, torrent.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return OnDependencyFailed(TorrentErrorKeys.Transmission, transmissionAddResult, transmissionAddError!);

        if (transmissionAddResult is TransmissionAddResult.Added)
        {
            var torrentName = GetTorrentUpdatedName(
                torrent.Name,
                transmissionAddTorrent.Name,
                transmissionAddTorrent.HashString,
                out var isNameBackgroundUpdateRequired);

            var torrentUpdateDto = transmissionAddTorrent.ToTorrentUpdateDto(DateTime.UtcNow, torrentName);
            var (updateResult, currentVersion) = await torrentService
                .UpdateOneAsync(torrent.Id, torrent.Version, torrentUpdateDto, cancellationToken)
                .ConfigureAwait(false);

            switch (updateResult)
            {
                case TorrentMutationResult.Success:
                    break;
                case TorrentMutationResult.NotFound:
                    return OnRemoved(transmissionAddResult);
                case TorrentMutationResult.VersionConflict:
                    return OnConflict(transmissionAddResult, currentVersion!.Value);
                case TorrentMutationResult.NotUnique:
                    return OnExists(transmissionAddResult);
                default:
                    throw new InvalidOperationException(
                        $"Unexpected {nameof(TorrentMutationResult)}: {updateResult}");
            }

            // Remove the old Transmission torrent only after the local row points at the new hash.
            var transmissionRemoveError = (await transmissionService
                .RemoveTorrentAsync(torrent.HashString, false, cancellationToken)
                .ConfigureAwait(false)).Error;

            torrent.HashString = torrentUpdateDto.HashString!;
            torrent.RefreshDate = torrentUpdateDto.RefreshDate!.Value;
            torrent.Name = torrentName ?? torrent.Name;
            torrent.Version = currentVersion!.Value;

            if (isNameBackgroundUpdateRequired)
            {
                _ = backgroundUpdateService.UpdateTorrentNameAsync(
                    id,
                    transmissionAddTorrent.HashString,
                    torrent.Name,
                    torrent.Version);
            }

            return OnRefreshed(torrent.ToDto(), transmissionAddResult, transmissionRemoveError);
        }
        else if (torrent.Name == torrent.HashString)
        {
            _ = backgroundUpdateService.UpdateTorrentNameAsync(
                id,
                torrent.HashString,
                torrent.Name,
                torrent.Version);
        }

        return OnRefreshed(torrent.ToDto(), transmissionAddResult);
    }

    private static RefreshTorrentByIdOutcome OnRefreshed(
        TorrentDto torrent,
        TransmissionAddResult? transmissionResult,
        string? warning = null) =>
        new(Result.Refreshed, torrent, transmissionResult, warning, []);

    private static RefreshTorrentByIdOutcome OnNotFoundLocally() =>
        new(Result.NotFoundLocally, null, null, null, [new(TorrentErrorKeys.Id, [EndpointMessages.NoSuchTorrent])]);

    private static RefreshTorrentByIdOutcome OnNotFoundInTransmission(string message) =>
        new(Result.NotFoundInTransmission, null, null, null, [new(TorrentErrorKeys.Transmission, [message])]);

    private static RefreshTorrentByIdOutcome OnDependencyFailed(
        string key,
        TransmissionAddResult? transmissionResult,
        string message) =>
        new(Result.DependencyFailed, null, transmissionResult, null, [new(key, [message])]);

    private static RefreshTorrentByIdOutcome OnInvalidConfiguration(string message) =>
        new(Result.InvalidConfiguration, null, null, null, [new(TorrentErrorKeys.Source, [message])]);

    private static RefreshTorrentByIdOutcome OnRemoved(TransmissionAddResult? transmissionResult) =>
        new(
            Result.Removed,
            null,
            transmissionResult,
            null,
            [new(TorrentErrorKeys.Id, [EndpointMessages.TorrentRemovedConflict])]);

    /// <remarks>
    /// The refreshed magnet has already been added to Transmission and the previous torrent has
    /// <b>not</b> been removed, because the local row could not be repointed at the new hash — another
    /// row already holds it. Per the project's independence-from-Transmission rule the partial
    /// outcome is surfaced as-is and the user retries; no compensating removal is performed.
    /// </remarks>
    private static RefreshTorrentByIdOutcome OnExists(TransmissionAddResult? transmissionResult) =>
        new(
            Result.Exists,
            null,
            transmissionResult,
            null,
            [new(TorrentErrorKeys.Id, [EndpointMessages.TorrentAlreadyExists])]);

    private static RefreshTorrentByIdOutcome OnConflict(
        TransmissionAddResult? transmissionResult,
        long currentVersion) =>
        new(
            Result.VersionConflict,
            null,
            transmissionResult,
            null,
            [new(TorrentErrorKeys.Version, [EndpointMessages.TorrentModifiedConflict])],
            currentVersion);

    private static string? GetTorrentUpdatedName(
        string oldName,
        string newTransmissionName,
        string newTransmissionHashString,
        out bool isBackgroundUpdateRequired)
    {
        if (newTransmissionName == newTransmissionHashString)
        {
            isBackgroundUpdateRequired = true;
            return null; // keep the old name for now, update in the background
        }

        if (string.IsNullOrWhiteSpace(newTransmissionName) || newTransmissionName == oldName)
        {
            isBackgroundUpdateRequired = false;
            return null; // no update required
        }

        isBackgroundUpdateRequired = false;
        return newTransmissionName;
    }
}
