using System.Text;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.Torrents.DeleteTorrentByIdResult;

namespace TransmissionManager.Api.Actions.Torrents;

internal sealed class DeleteTorrentByIdHandler(
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentSchedulerService scheduler)
{
    private const string _noSuchTorrent = "No such torrent.";

    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Removal of the torrent with id {0} has failed: '{1}'.");

    public async Task<DeleteTorrentByIdOutcome> TryDeleteTorrentByIdAsync(
        long id,
        DeleteTorrentByIdType deleteType,
        CancellationToken cancellationToken)
    {
        if (deleteType is DeleteTorrentByIdType.Local)
        {
            scheduler.TryUnscheduleTorrentRefresh(id);
            return await torrentService.TryDeleteOneByIdAsync(id, cancellationToken).ConfigureAwait(false)
                ? new(Result.Removed, null)
                : new(Result.NotFoundLocally, GetError(id, _noSuchTorrent));
        }

        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFoundLocally, GetError(id, _noSuchTorrent));

        var deleteData = deleteType is DeleteTorrentByIdType.LocalAndTransmissionAndData;
        var transmissionResult = await transmissionService
            .RemoveTorrentAsync(torrent.HashString, deleteData, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionResult.Error is not null)
            return new(Result.DependencyFailed, GetError(id, transmissionResult.Error));

        scheduler.TryUnscheduleTorrentRefresh(id);
        await torrentService.TryDeleteOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return new(Result.Removed, null);
    }

    private static string GetError(long id, string? message) =>
        string.Format(null, _error, id, message);
}
