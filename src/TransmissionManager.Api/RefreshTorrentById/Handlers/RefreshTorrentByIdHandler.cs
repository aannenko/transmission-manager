using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.RefreshTorrentById.Extensions;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.RefreshTorrentById.Handlers.RefreshTorrentByIdResult.ResultType;

namespace TransmissionManager.Api.RefreshTorrentById.Handlers;

public sealed class RefreshTorrentByIdHandler(
    TorrentWebPageService torrentWebPageService,
    TransmissionService transmissionService,
    TorrentQueryService queryService,
    TorrentCommandService commandService,
    TorrentNameUpdateService torrentNameUpdateService)
{
    public async Task<RefreshTorrentByIdResult> RefreshTorrentByIdAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        const string error = "Refresh of the torrent with id '{0}' has failed: '{1}'.";
        var torrent = await queryService.FindOneByIdAsync(torrentId, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFound, string.Format(error, torrentId, "No such torrent."));

        var (transmissionGetTorrent, transmissionGetError) = await transmissionService.GetTorrentFromTransmissionAsync(
            torrent.HashString,
            cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetTorrent is null)
            return new(Result.Error, string.Format(error, torrentId, transmissionGetError));

        var (magnetUri, getMagnetError) = await torrentWebPageService.GetMagnetUriAsync(
            torrent.WebPageUri,
            torrent.MagnetRegexPattern,
            cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(Result.Error, string.Format(error, torrentId, getMagnetError));

        var (transmissionAddTorrent, transmissionAddError) = await transmissionService.SendMagnetToTransmissionAsync(
            magnetUri,
            torrent.DownloadDir,
            cancellationToken)
            .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return new(Result.Error, string.Format(error, torrentId, transmissionAddError));

        var updateDto = transmissionAddTorrent.ToTorrentUpdateDto();
        var isUpdated = await commandService.TryUpdateOneByIdAsync(torrent.Id, updateDto, cancellationToken)
            .ConfigureAwait(false);

        if (!isUpdated)
        {
            var message = $"Torrent with id {torrentId} was removed before it could be updated.";
            return new(Result.Error, string.Format(error, torrentId, message));
        }

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = torrentNameUpdateService.StartUpdateTorrentNameTask(torrentId, transmissionAddTorrent.HashString);

        return new(Result.Success, null);
    }
}
