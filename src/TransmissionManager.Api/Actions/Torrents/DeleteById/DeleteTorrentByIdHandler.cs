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
            return OnNotFound();

        if (torrent.Version != version)
            return OnConflict(torrent.Version);

        var deleteData = deleteType is DeleteTorrentByIdType.LocalAndTransmissionAndData;
        var transmissionResult = await transmissionService
            .RemoveTorrentAsync(torrent.HashString, deleteData, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionResult.Error is not null)
            return OnDependencyFailed(transmissionResult.Error);

        return await RemoveLocal(id, version, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DeleteTorrentByIdOutcome> RemoveLocal(long id, long version, CancellationToken cancellationToken)
    {
        var result = await torrentService.DeleteOneAsync(id, version, cancellationToken).ConfigureAwait(false);
        return result.Result switch
        {
            TorrentMutationResult.Success => OnRemoved(id),
            TorrentMutationResult.NotFound => OnNotFound(),
            TorrentMutationResult.VersionConflict => OnConflict(result.CurrentVersion!.Value),
            _ => throw new InvalidOperationException($"Unexpected {nameof(TorrentMutationResult)}: {result}")
        };
    }

    private DeleteTorrentByIdOutcome OnRemoved(long id)
    {
        _ = scheduler.TryUnscheduleTorrentRefresh(id);
        return new(Result.Deleted, null, []);
    }

    private static DeleteTorrentByIdOutcome OnNotFound() =>
        new(Result.NotFound, null, [new(TorrentErrorKeys.Id, [EndpointMessages.NoSuchTorrent])]);

    private static DeleteTorrentByIdOutcome OnConflict(long version) =>
        new(Result.VersionConflict, version, [new(TorrentErrorKeys.Version, [EndpointMessages.TorrentModifiedConflict])]);

    private static DeleteTorrentByIdOutcome OnDependencyFailed(string message) =>
        new(Result.DependencyFailed, null, [new(TorrentErrorKeys.Transmission, [message])]);
}
