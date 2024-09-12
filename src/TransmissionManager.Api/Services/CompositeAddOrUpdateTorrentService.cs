﻿using TransmissionManager.Api.Dto;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentTrackerClient.Services;
using TransmissionManager.TransmissionClient.Services;
using Result = TransmissionManager.Api.Dto.AddOrUpdateTorrentResult.ResultType;

namespace TransmissionManager.Api.Services;

public sealed class CompositeAddOrUpdateTorrentService(
    TorrentWebPageService torrentWebPageService,
    TransmissionService transmissionService,
    TorrentQueryService queryService,
    SchedulableTorrentCommandService commandService,
    BackgroundTaskService backgroundTaskService)
    : BaseCompositeTorrentService(torrentWebPageService, transmissionService, backgroundTaskService)
{
    public async Task<AddOrUpdateTorrentResult> AddOrUpdateTorrentAsync(
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        const string error = "Addition or update of a torrent from the web page '{0}' has failed: {1}.";
        var (magnetUri, trackerError) =
            await GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(Result.Error, -1, string.Format(error, dto.WebPageUri, trackerError));

        var (transmissionTorrent, transmissionError) =
            await SendMagnetToTransmissionAsync(magnetUri, dto.DownloadDir, cancellationToken).ConfigureAwait(false);

        if (transmissionTorrent is null)
            return new(Result.Error, -1, string.Format(error, dto.WebPageUri, transmissionError));

        var torrents = await queryService.FindPageAsync(new(1, 0), new(dto.WebPageUri), cancellationToken)
            .ConfigureAwait(false);

        var torrentId = torrents.FirstOrDefault()?.Id ?? -1;
        Result resultType;
        if (torrentId is -1)
        {
            resultType = Result.Add;
            var addDto = dto.ToTorrentAddDto(transmissionTorrent);
            torrentId = await commandService.AddOneAsync(addDto, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            resultType = Result.Update;
            var updateDto = dto.ToTorrentUpdateDto(transmissionTorrent);
            var isUpdated = await commandService.TryUpdateOneByIdAsync(torrentId, updateDto, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdated)
            {
                var message = $"Torrent with id {torrentId} was removed before it could be updated.";
                return new(Result.Error, torrentId, string.Format(error, dto.WebPageUri, message));
            }
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId, transmissionTorrent.HashString);

        return new(resultType, torrentId, null);
    }
}
