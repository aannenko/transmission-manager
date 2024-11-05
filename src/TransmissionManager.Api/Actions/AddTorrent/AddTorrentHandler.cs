using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Shared.Scheduling;
using TransmissionManager.Api.Shared.Services;
using TransmissionManager.Api.Shared.TorrentWebPage;
using TransmissionManager.Api.Shared.Transmission;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.AddTorrent.AddTorrentResult;

namespace TransmissionManager.Api.Actions.AddTorrent;

public sealed partial class AddTorrentHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentCommandService torrentCommandService,
    TorrentSchedulerService schedulerService,
    TorrentNameUpdateService torrentNameUpdateService)
{
    public async Task<AddTorrentOutcome> AddTorrentAsync(AddTorrentRequest dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        const string error = "Addition of a torrent from the web page '{0}' has failed: '{1}'.";

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
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
            var torrentExistsError = string.Format(error, dto.WebPageUri, "Torrent already exists.");
            return new(Result.TorrentExists, null, transmissionResult, torrentExistsError);
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = torrentNameUpdateService.UpdateTorrentNameAsync(torrentId, transmissionTorrent.HashString);

        return new(Result.TorrentAdded, torrentId, transmissionResult, null);
    }
}
