using Microsoft.EntityFrameworkCore;
using System.Text;
using TransmissionManager.Api.Common.Scheduling;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.Common.TorrentWebPage;
using TransmissionManager.Api.Common.Transmission;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.AddTorrent.AddTorrentResult;

namespace TransmissionManager.Api.Actions.AddTorrent;

internal sealed partial class AddTorrentHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentSchedulerService schedulerService,
    TorrentNameUpdateService torrentNameUpdateService)
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Addition of a torrent from the web page '{0}' has failed: '{1}'.");

    public async Task<AddTorrentOutcome> AddTorrentAsync(AddTorrentRequest dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return new(Result.DependencyFailed, null, null, GetError(dto.WebPageUri, getMagnetError));

        var (transmissionResult, transmissionTorrent, transmissionError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, dto.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionTorrent is null)
            return new(Result.DependencyFailed, null, null, GetError(dto.WebPageUri, transmissionError));

        long torrentId;
        try
        {
            torrentId = await torrentService
                .AddOneAsync(dto.ToTorrentAddDto(transmissionTorrent), cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(dto.Cron))
                schedulerService.ScheduleTorrentRefresh(torrentId, dto.Cron);
        }
        catch (DbUpdateException)
        {
            var torrentExistsError = GetError(dto.WebPageUri, "Torrent already exists.");
            return new(Result.TorrentExists, null, transmissionResult, torrentExistsError);
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = torrentNameUpdateService.UpdateTorrentNameAsync(torrentId, transmissionTorrent.HashString);

        return new(Result.TorrentAdded, torrentId, transmissionResult, null);
    }

    private static string GetError(Uri webPageUri, string? message) =>
        string.Format(null, _error, webPageUri, message);
}
