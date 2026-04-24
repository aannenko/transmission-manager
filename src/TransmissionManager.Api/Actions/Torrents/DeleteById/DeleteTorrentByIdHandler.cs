using System.Globalization;
using System.Text;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.Torrents.DeleteById.DeleteTorrentByIdResult;

namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal sealed class DeleteTorrentByIdHandler(
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentSchedulerService scheduler)
{
    private const string _noSuchTorrent = "No such torrent.";
    private const string _concurrencyConflict =
        "The torrent was modified by another request. Re-read it and retry the deletion.";

    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Removal of the torrent with id {0} has failed: '{1}'.");

    public async Task<DeleteTorrentByIdOutcome> TryDeleteTorrentByIdAsync(
        long id,
        DeleteTorrentByIdType deleteType,
        uint? expectedVersion,
        CancellationToken cancellationToken)
    {
        if (deleteType is DeleteTorrentByIdType.Local)
        {
            _ = scheduler.TryUnscheduleTorrentRefresh(id);
            var localResult = await torrentService
                .TryDeleteOneByIdAsync(id, expectedVersion, cancellationToken)
                .ConfigureAwait(false);

            return localResult switch
            {
                TorrentDeleteResult.Deleted => new(Result.Removed, null),
                TorrentDeleteResult.NotFound => new(Result.NotFoundLocally, GetError(id, _noSuchTorrent)),
                TorrentDeleteResult.ConcurrencyConflict =>
                    new(Result.ConcurrencyConflict, GetError(id, _concurrencyConflict)),
                _ => throw new NotImplementedException(),
            };
        }

        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFoundLocally, GetError(id, _noSuchTorrent));

        if (expectedVersion is not null && torrent.Version != expectedVersion)
            return new(Result.ConcurrencyConflict, GetError(id, _concurrencyConflict));

        var deleteData = deleteType is DeleteTorrentByIdType.LocalAndTransmissionAndData;
        var transmissionResult = await transmissionService
            .RemoveTorrentAsync(torrent.HashString, deleteData, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionResult.Error is not null)
            return new(Result.DependencyFailed, GetError(id, transmissionResult.Error));

        _ = scheduler.TryUnscheduleTorrentRefresh(id);
        var dbResult = await torrentService
            .TryDeleteOneByIdAsync(id, expectedVersion, cancellationToken)
            .ConfigureAwait(false);

        return dbResult is TorrentDeleteResult.ConcurrencyConflict
            ? new(Result.ConcurrencyConflict, GetError(id, _concurrencyConflict))
            : new(Result.Removed, null);
    }

    private static string GetError(long id, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, id, message);
}
