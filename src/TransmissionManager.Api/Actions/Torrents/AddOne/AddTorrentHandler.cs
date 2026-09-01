using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using DbSourceKind = TransmissionManager.Database.Dto.TorrentSourceKind;
using Outcome = TransmissionManager.Api.Actions.Torrents.AddOne.AddTorrentOutcome;
using Result = TransmissionManager.Api.Actions.Torrents.AddOne.AddTorrentResult;

namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal sealed class AddTorrentHandler(
    IServiceProvider serviceProvider,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentSchedulerService schedulerService,
    BackgroundTorrentUpdateService backgroundUpdateService)
{
    public async Task<Outcome> AddTorrentAsync(AddTorrentRequest request, CancellationToken cancellationToken)
    {
        var sourceKind = (DbSourceKind)request.SourceKind;

        var (searchResult, magnetUri, getMagnetError) = await serviceProvider
            .FindMagnetUriAsync(
                request.SourceUri,
                sourceKind,
                request.MagnetRegexPattern,
                request.JsonValueFormat,
                cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
        {
            return searchResult.IsUnprocessableSource()
                ? OnInvalidRequest(getMagnetError!)
                : OnDependencyFailed(TorrentErrorKeys.Source, getMagnetError!);
        }

        var (transmissionResult, transmissionTorrent, transmissionError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, request.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionTorrent is null)
            return OnDependencyFailed(TorrentErrorKeys.Transmission, transmissionError!);

        var (addResult, torrent) = await torrentService
            .AddOneAsync(request.ToTorrentAddDto(transmissionTorrent, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        if (addResult is TorrentMutationResult.NotUnique)
            return OnExists(transmissionResult);

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

        return new(Result.Added, torrent.ToDto(), transmissionResult, []);
    }

    /// <remarks>
    /// The address is the only thing a caller can act on: the collision is on the source address or
    /// on the hash the source resolved to, and the storage layer does not say which.
    /// </remarks>
    private static Outcome OnExists(TransmissionAddResult? result) =>
        new(Result.Exists, null, result, [new(TorrentErrorKeys.SourceUri, [EndpointMessages.TorrentAlreadyExists])]);

    private static Outcome OnDependencyFailed(string key, string message) =>
        new(Result.DependencyFailed, null, null, [new(key, [message])]);

    private static Outcome OnInvalidRequest(string message) =>
        new(Result.InvalidRequest, null, null, [new(TorrentErrorKeys.Source, [message])]);
}
