using TransmissionManager.Api.Dto;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentTrackerClient.Services;
using TransmissionManager.TransmissionClient.Services;
using Result = TransmissionManager.Api.Dto.RefreshTorrentResult.ResultType;

namespace TransmissionManager.Api.Services;

public sealed class CompositeRefreshTorrentService(
    TorrentWebPageService torrentWebPageService,
    TransmissionService transmissionService,
    TorrentService torrentService,
    BackgroundTaskService backgroundTaskService)
    : BaseCompositeTorrentService(torrentWebPageService, transmissionService, backgroundTaskService)
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
            await GetTorrentFromTransmissionAsync(torrent.HashString, cancellationToken).ConfigureAwait(false);

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

        var updateDto = transmissionAddTorrent.ToTorrentUpdateDto();
        if (!await torrentService.TryUpdateOneByIdAsync(torrent.Id, updateDto, cancellationToken).ConfigureAwait(false))
        {
            var formattedError = string.Format(
                    error,
                    torrent.WebPageUri,
                    $"Torrent with id {torrentId} was removed before it could be updated.");

            return new(Result.NotFound, string.Format(error, torrentId, formattedError));
        }

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId, transmissionAddTorrent.HashString);

        return new(Result.Success, null);
    }
}
