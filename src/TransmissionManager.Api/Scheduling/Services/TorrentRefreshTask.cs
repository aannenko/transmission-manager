﻿using Coravel.Invocable;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class TorrentRefreshTask(
    ILogger<TorrentRefreshTask> logger,
    CompositeTorrentService<TorrentService> compositeService,
    long torrentId)
    : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        var (_, errorMessage) = await compositeService.RefreshTorrentAsync(torrentId, CancellationToken);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger.LogError(
                "Could not refresh the torrent with id '{torrentId}' on schedule: '{errorMessage}'.",
                torrentId,
                errorMessage);
        }
    }
}
