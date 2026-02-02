using System.Globalization;
using System.Text;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.TorrentWebPage;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.Torrents.RefreshById.RefreshTorrentByIdResult;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal sealed class RefreshTorrentByIdHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    BackgroundTorrentUpdateService backgroundUpdateService)
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Refresh of the torrent with id {0} has failed: '{1}'.");

    public async Task<RefreshTorrentByIdOutcome> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken)
    {
        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFoundLocally, null, null, GetError(id, "No such torrent."));

        var (_, transmissionGetError) = await transmissionService
            .GetTorrentAsync(torrent.HashString, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetError is not null)
            return new(Result.NotFoundInTransmission, null, null, GetError(id, transmissionGetError));

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(new(torrent.WebPageUri), torrent.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return new(Result.DependencyFailed, null, null, GetError(id, getMagnetError));

        var (transmissionAddResult, transmissionAddTorrent, transmissionAddError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, torrent.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return new(Result.DependencyFailed, null, transmissionAddResult, GetError(id, transmissionAddError));

        if (transmissionAddResult is TransmissionAddResult.Added)
        {
            var transmissionRemoveTask = transmissionService
                .RemoveTorrentAsync(torrent.HashString, false, cancellationToken);

            var torrentName = GetTorrentUpdatedName(
                torrent.Name,
                transmissionAddTorrent.Name,
                transmissionAddTorrent.HashString,
                out var isNameBackgroundUpdateRequired);

            var torrentUpdateDto = transmissionAddTorrent.ToTorrentUpdateDto(DateTime.UtcNow, torrentName);
            var isTorrentUpdated = await torrentService
                .TryUpdateOneByIdAsync(torrent.Id, torrentUpdateDto, cancellationToken)
                .ConfigureAwait(false);

            if (!isTorrentUpdated)
            {
                const string message = "The torrent was removed before it could be updated.";
                return new(Result.Removed, null, transmissionAddResult, GetError(id, message));
            }

            if (isNameBackgroundUpdateRequired)
                _ = backgroundUpdateService.UpdateTorrentNameAsync(id, transmissionAddTorrent.HashString, torrent.Name);

            var transmissionRemoveError = (await transmissionRemoveTask.ConfigureAwait(false)).Error;
            if (transmissionRemoveError is not null)
                return new(Result.DependencyFailed, null, transmissionAddResult, GetError(id, transmissionRemoveError));

            torrent.HashString = torrentUpdateDto.HashString!;
            torrent.RefreshDate = torrentUpdateDto.RefreshDate!.Value;
            torrent.Name = torrentName ?? torrent.Name;
        }
        else if (torrent.Name == torrent.HashString)
        {
            _ = backgroundUpdateService.UpdateTorrentNameAsync(id, torrent.HashString, torrent.Name);
        }

        return new(Result.Refreshed, torrent.ToDto(), transmissionAddResult, null);
    }

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
