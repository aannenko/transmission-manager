using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Database.Services;
using static TransmissionManager.Api.Common.Services.TransmissionClientWrapper;

namespace TransmissionManager.Api.AddTorrent;

public sealed class AddTorrentHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentCommandService torrentCommandService,
    TorrentSchedulerService schedulerService,
    TorrentNameUpdateService torrentNameUpdateService)
{
    public enum Result
    {
        Success,
        TorrentExists,
        DependencyFailed
    }

    public readonly record struct Response(
        Result Result,
        long? Id,
        TransmissionAddResult? TransmissionResult,
        string? Error);

    public async Task<Response> AddTorrentAsync(AddTorrentRequest dto, CancellationToken cancellationToken = default)
    {
        const string error = "Addition of a torrent from the web page '{0}' has failed: '{1}'.";

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(Result.DependencyFailed, null, null, string.Format(error, dto.WebPageUri, getMagnetError));

        var (transmissionResult, transmissionTorrent, transmissionError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, dto.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionTorrent is null)
            return new(Result.DependencyFailed, null, null, string.Format(error, dto.WebPageUri, transmissionError));

        long torrentId;
        try
        {
            torrentId = await torrentCommandService
                .AddOneAsync(dto.ToTorrentAddDto(transmissionTorrent), cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(dto.Cron))
                schedulerService.ScheduleTorrentRefresh(torrentId, dto.Cron);
        }
        catch (DbUpdateException)
        {
            return new(Result.TorrentExists, null, transmissionResult, "Torrent already exists.");
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = torrentNameUpdateService.StartUpdateTorrentNameTask(torrentId, transmissionTorrent.HashString);

        return new(Result.Success, torrentId, transmissionResult, null);
    }
}
