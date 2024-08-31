using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;
using TransmissionManager.TorrentTrackers.Services;
using TransmissionManager.Transmission.Services;
using Result = TransmissionManager.Api.Endpoints.Dto.RefreshTorrentResult.ResultType;

namespace TransmissionManager.Api.Endpoints.Services;

public sealed class CompositeRefreshTorrentService(
    MagnetUriRetriever magnetRetriever,
    TransmissionService transmissionService,
    TorrentService torrentService,
    BackgroundTaskService backgroundTaskService)
    : BaseCompositeTorrentService(magnetRetriever, transmissionService, backgroundTaskService)
{
    public async Task<RefreshTorrentResult> RefreshTorrentAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        const string error = "Refresh of the torrent with id '{0}' has failed: '{1}'.";
        var torrent = await torrentService.FindOneByIdAsync(torrentId, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFound, string.Format(error, torrentId, "No such torrent."));

        var (transmissionGetTorrent, transmissionGetError) =
            await GetTorrentFromTransmissionAsync(torrent.TransmissionId, cancellationToken).ConfigureAwait(false);

        if (transmissionGetTorrent is null)
            return new(Result.Error, string.Format(error, torrentId, transmissionGetError));

        var (magnetUri, trackerError) =
            await GetMagnetUriAsync(torrent.WebPageUri, torrent.MagnetRegexPattern, cancellationToken)
                .ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(Result.Error, string.Format(error, torrentId, trackerError));

        var (transmissionAddTorrent, transmissionAddError) =
            await SendMagnetToTransmissionAsync(magnetUri, torrent.DownloadDir, cancellationToken)
                .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return new(Result.Error, string.Format(error, torrentId, transmissionAddError));

        var updateDto = torrent.ToTorrentUpdateDto(transmissionAddTorrent);
        if (!await torrentService.TryUpdateOneByIdAsync(torrent.Id, updateDto, cancellationToken).ConfigureAwait(false))
        {
            var formattedError = string.Format(
                    error,
                    torrent.WebPageUri,
                    $"Torrent with id {torrentId} was removed before it could be updated.");

            return new(Result.NotFound, string.Format(error, torrentId, formattedError));
        }

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId, updateDto);

        return new(Result.Success, null);
    }
}
