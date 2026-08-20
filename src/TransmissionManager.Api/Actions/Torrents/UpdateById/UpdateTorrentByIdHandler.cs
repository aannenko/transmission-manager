using System.Globalization;
using System.Text;
using TransmissionManager.Api.Common.Dto.Torrents;
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
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dto = request.ToTorrentUpdateDto();

        var (result, currentVersion) = await torrentService
            .UpdateOneAsync(id, version, dto, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            TorrentMutationResult.Success => OnUpdated(id, dto.Cron),
            TorrentMutationResult.NotFound => OnNotFound(id),
            TorrentMutationResult.VersionConflict =>
                OnConflict(id, EndpointMessages.TorrentModifiedConflict, currentVersion),
            TorrentMutationResult.NotUnique => // unreachable - we don't change anything unique in a torrent
                OnConflict(id, EndpointMessages.TorrentAlreadyExists, currentVersion),
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

    private static UpdateTorrentByIdOutcome OnConflict(long id, string? message, long? currentVersion) =>
        new(UpdateTorrentByIdResult.Conflict, currentVersion, GetError(id, message));

    private static string GetError(long id, string? message) =>
        string.Format(CultureInfo.InvariantCulture, _error, id, message);
}
