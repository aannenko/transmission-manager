﻿using TransmissionManager.Api.Composite.Dto;
using TransmissionManager.Api.Composite.Extensions;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Services;
using RefreshResult = TransmissionManager.Api.Composite.Dto.RefreshTorrentResult.ResultType;

namespace TransmissionManager.Api.Composite.Services;

public sealed class CompositeRefreshTorrentService(
    MagnetUriRetriever magnetRetriever,
    TransmissionClient transmissionClient,
    TorrentService torrentService,
    BackgroundTaskService backgroundTaskService)
    : BaseCompositeTorrentService(magnetRetriever, transmissionClient, backgroundTaskService)
{
    public async Task<RefreshTorrentResult> RefreshTorrentAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        const string error = "Refresh of the torrent with id '{0}' has failed: '{1}'.";
        var torrent = await torrentService.FindOneByIdAsync(torrentId).ConfigureAwait(false);
        if (torrent is null)
            return new(RefreshResult.NotFound, string.Format(error, torrentId, "No such torrent."));

        var (transmissionGetTorrent, transmissionGetError) =
            await GetTorrentFromTransmissionAsync(torrent.TransmissionId, cancellationToken).ConfigureAwait(false);

        if (transmissionGetTorrent is null)
            return new(RefreshResult.Error, string.Format(error, torrentId, transmissionGetError));

        var (magnetUri, trackerError) =
            await GetMagnetUriAsync(torrent.WebPageUri, torrent.MagnetRegexPattern, cancellationToken)
                .ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(RefreshResult.Error, string.Format(error, torrentId, trackerError));

        var (transmissionAddTorrent, transmissionAddError) =
            await SendMagnetToTransmissionAsync(magnetUri, torrent.DownloadDir, cancellationToken)
                .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return new(RefreshResult.Error, string.Format(error, torrentId, transmissionAddError));

        var updateDto = torrent.ToTorrentUpdateDto(transmissionAddTorrent);
        if (!await torrentService.TryUpdateOneByIdAsync(torrent.Id, updateDto).ConfigureAwait(false))
            return new(RefreshResult.NotFound, string.Format(error, torrentId, "No such torrent."));

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId, updateDto);

        return new(RefreshResult.Success, null);
    }
}
