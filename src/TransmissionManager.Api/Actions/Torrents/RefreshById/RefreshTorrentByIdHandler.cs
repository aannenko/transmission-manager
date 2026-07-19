using System.Globalization;
using System.Text;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.TorrentWebPage;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.Torrents.RefreshById.RefreshTorrentByIdResult;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal sealed class RefreshTorrentByIdHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    BackgroundTorrentUpdateService backgroundUpdateService)
    : IRefreshTorrentByIdHandler
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Torrent '{0}' refresh failed: '{1}'.");

    public async Task<RefreshTorrentByIdOutcome> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken)
    {
        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return OnNotFoundLocally(id);

        var (_, transmissionGetError) = await transmissionService
            .GetTorrentAsync(torrent.HashString, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetError is not null)
            return OnNotFoundInTransmission(id, transmissionGetError);

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(new(torrent.WebPageUri), torrent.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return OnDependencyFailed(id, null, getMagnetError);

        var (transmissionAddResult, transmissionAddTorrent, transmissionAddError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, torrent.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return OnDependencyFailed(id, transmissionAddResult, transmissionAddError);

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
                    return OnRemoved(id, transmissionAddResult);
                case TorrentMutationResult.Conflict:
                    return OnConflict(id, transmissionAddResult, currentVersion!.Value);
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
        string? message = null) =>
        new(Result.Refreshed, torrent, transmissionResult, message);

    private static RefreshTorrentByIdOutcome OnNotFoundLocally(long id) =>
        new(Result.NotFoundLocally, null, null, GetError(id, EndpointMessages.NoSuchTorrent));

    private static RefreshTorrentByIdOutcome OnNotFoundInTransmission(long id, string message) =>
        new(Result.NotFoundInTransmission, null, null, GetError(id, message));

    private static RefreshTorrentByIdOutcome OnDependencyFailed(
        long id,
        TransmissionAddResult? transmissionResult,
        string? message) =>
        new(Result.DependencyFailed, null, transmissionResult, GetError(id, message));

    private static RefreshTorrentByIdOutcome OnRemoved(long id, TransmissionAddResult? transmissionResult) =>
        new(Result.Removed, null, transmissionResult, GetError(id, EndpointMessages.TorrentRemovedConflict));

    private static RefreshTorrentByIdOutcome OnConflict(
        long id,
        TransmissionAddResult? transmissionResult,
        long currentVersion) =>
        new(
            Result.Conflict,
            null,
            transmissionResult,
            GetError(id, EndpointMessages.TorrentModifiedConflict),
            currentVersion);

    private static string GetError(long id, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, id, message);

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
