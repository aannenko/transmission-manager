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
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Torrent '{0}' deletion failed: '{1}'.");

    public async Task<DeleteTorrentByIdOutcome> TryDeleteTorrentByIdAsync(
        long id,
        long version,
        DeleteTorrentByIdType deleteType,
        CancellationToken cancellationToken)
    {
        if (deleteType is DeleteTorrentByIdType.Local)
            return await RemoveLocal(id, version, cancellationToken).ConfigureAwait(false);

        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return OnNotFound(id);

        if (torrent.Version != version)
            return OnConflict(id, torrent.Version);

        var deleteData = deleteType is DeleteTorrentByIdType.LocalAndTransmissionAndData;
        var transmissionResult = await transmissionService
            .RemoveTorrentAsync(torrent.HashString, deleteData, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionResult.Error is not null)
            return OnDependencyFailed(id, transmissionResult.Error);

        return await RemoveLocal(id, version, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DeleteTorrentByIdOutcome> RemoveLocal(long id, long version, CancellationToken cancellationToken)
    {
        var result = await torrentService.DeleteOneAsync(id, version, cancellationToken).ConfigureAwait(false);
        return result.Result switch
        {
            TorrentMutationResult.Success => OnRemoved(id),
            TorrentMutationResult.NotFound => OnNotFound(id),
            TorrentMutationResult.Conflict => OnConflict(id, result.CurrentVersion!.Value),
            _ => throw new InvalidOperationException($"Unexpected {nameof(TorrentMutationResult)}: {result}")
        };
    }

    private DeleteTorrentByIdOutcome OnRemoved(long id)
    {
        _ = scheduler.TryUnscheduleTorrentRefresh(id);
        return new(Result.Deleted, null, null);
    }

    private static DeleteTorrentByIdOutcome OnNotFound(long id) =>
        new(Result.NotFound, null, GetError(id, EndpointMessages.NoSuchTorrent));

    private static DeleteTorrentByIdOutcome OnConflict(long id, long version) =>
        new(Result.Conflict, version, GetError(id, EndpointMessages.TorrentModifiedConflict));

    private static DeleteTorrentByIdOutcome OnDependencyFailed(long id, string message) =>
        new(Result.DependencyFailed, null, GetError(id, message));

    private static string GetError(long id, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, id, message);
}
