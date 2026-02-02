using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.TorrentWebPage;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.Torrents.AddOne.AddTorrentResult;

namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal sealed class AddTorrentHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentSchedulerService schedulerService,
    BackgroundTorrentUpdateService backgroundUpdateService)
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Addition of a torrent from the web page '{0}' has failed: '{1}'.");

    public async Task<AddTorrentOutcome> AddTorrentAsync(AddTorrentRequest request, CancellationToken cancellationToken)
    {
        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(request.WebPageUri, request.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return new(Result.DependencyFailed, null, null, GetError(request.WebPageUri, getMagnetError));

        var (transmissionResult, transmissionTorrent, transmissionError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, request.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionTorrent is null)
            return new(Result.DependencyFailed, null, null, GetError(request.WebPageUri, transmissionError));

        Torrent torrent;
        try
        {
            torrent = await torrentService
                .AddOneAsync(request.ToTorrentAddDto(transmissionTorrent, DateTime.UtcNow), cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(request.Cron))
                schedulerService.ScheduleTorrentRefresh(torrent.Id, request.Cron);
        }
        catch (DbUpdateException)
        {
            var torrentExistsError = GetError(request.WebPageUri, "Torrent already exists.");
            return new(Result.Exists, null, transmissionResult, torrentExistsError);
        }

        if (torrent.Name == torrent.HashString)
            _ = backgroundUpdateService.UpdateTorrentNameAsync(torrent.Id, torrent.HashString, torrent.Name);

        return new(Result.Added, torrent.ToDto(), transmissionResult, null);
    }

    private static string GetError(Uri webPageUri, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, webPageUri, message);
}
