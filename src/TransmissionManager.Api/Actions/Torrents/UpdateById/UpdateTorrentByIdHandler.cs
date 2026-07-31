using System.Globalization;
using System.Text;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal sealed class UpdateTorrentByIdHandler(TorrentService torrentService, TorrentSchedulerService scheduler)
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Torrent '{0}' update failed: '{1}'.");

    public async Task<UpdateTorrentByIdOutcome> TryUpdateTorrentByIdAsync(
        long id,
        long version,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var (result, currentVersion) = await torrentService
            .UpdateOneAsync(id, version, dto, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            TorrentMutationResult.Success => OnUpdated(id, dto.Cron),
            TorrentMutationResult.NotFound => OnNotFound(id),
            TorrentMutationResult.VersionConflict => OnConflict(id, currentVersion!.Value),
            TorrentMutationResult.NotUnique => OnExists(id),
            _ => throw new InvalidOperationException($"Unexpected {nameof(TorrentMutationResult)}: {result}")
        };
    }

    private UpdateTorrentByIdOutcome OnUpdated(long id, string? cron)
    {
        if (cron is not null)
        {
            _ = scheduler.TryUnscheduleTorrentRefresh(id);
            if (cron.Length > 0)
                scheduler.ScheduleTorrentRefresh(id, cron);
        }

        return new(UpdateTorrentByIdResult.Updated, null, null);
    }

    private static UpdateTorrentByIdOutcome OnNotFound(long id) =>
        new(UpdateTorrentByIdResult.NotFound, null, GetError(id, EndpointMessages.NoSuchTorrent));

    private static UpdateTorrentByIdOutcome OnConflict(long id, long version) =>
        new(UpdateTorrentByIdResult.VersionConflict, version, GetError(id, EndpointMessages.TorrentModifiedConflict));

    private static UpdateTorrentByIdOutcome OnExists(long id) =>
        new(UpdateTorrentByIdResult.Exists, null, GetError(id, EndpointMessages.TorrentAlreadyExists));

    private static string GetError(long id, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, id, message);
}
