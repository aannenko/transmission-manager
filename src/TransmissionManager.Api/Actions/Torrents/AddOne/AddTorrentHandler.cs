using System.Globalization;
using System.Text;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentSources.WebPage;
using Outcome = TransmissionManager.Api.Actions.Torrents.AddOne.AddTorrentOutcome;
using Result = TransmissionManager.Api.Actions.Torrents.AddOne.AddTorrentResult;

namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal sealed class AddTorrentHandler(
    TorrentWebPageClient torrentWebPageClient,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentSchedulerService schedulerService,
    BackgroundTorrentUpdateService backgroundUpdateService)
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Torrent '{0}' addition failed: '{1}'.");

    public async Task<Outcome> AddTorrentAsync(AddTorrentRequest request, CancellationToken cancellationToken)
    {
        var (searchResult, magnetUri, getMagnetError) = await torrentWebPageClient
            .FindMagnetUriAsync(request.WebPageUri, request.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
        {
            return searchResult.IsInvalidInput()
                ? OnInvalidRequest(request.WebPageUri, getMagnetError)
                : OnDependencyFailed(request.WebPageUri, null, getMagnetError);
        }

        var (transmissionResult, transmissionTorrent, transmissionError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, request.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionTorrent is null)
            return OnDependencyFailed(request.WebPageUri, null, transmissionError);

        var (addResult, torrent) = await torrentService
            .AddOneAsync(request.ToTorrentAddDto(transmissionTorrent, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        if (addResult is TorrentMutationResult.NotUnique)
            return OnExists(request.WebPageUri, transmissionResult);

        return OnAdded(torrent!, transmissionResult, request.Cron);
    }

    private Outcome OnAdded(Torrent torrent, TransmissionAddResult? transmissionResult, string? cron)
    {
        if (!string.IsNullOrEmpty(cron))
            schedulerService.ScheduleTorrentRefresh(torrent.Id, cron);

        if (torrent.Name == torrent.HashString)
        {
            _ = backgroundUpdateService
                .UpdateTorrentNameAsync(torrent.Id, torrent.HashString, torrent.Name, torrent.Version);
        }

        return new(Result.Added, torrent.ToDto(), transmissionResult, null);
    }

    private static Outcome OnExists(Uri webPageUri, TransmissionAddResult? result) =>
        new(Result.Exists, null, result, GetError(webPageUri, EndpointMessages.TorrentAlreadyExists));

    private static Outcome OnDependencyFailed(Uri webPageUri, TransmissionAddResult? result, string? message) =>
        new(Result.DependencyFailed, null, result, GetError(webPageUri, message));

    private static Outcome OnInvalidRequest(Uri webPageUri, string? message) =>
        new(Result.InvalidRequest, null, null, GetError(webPageUri, message));

    private static string GetError(Uri webPageUri, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, webPageUri, message);
}
