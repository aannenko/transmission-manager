using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Validation;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using ApiSourceKind = TransmissionManager.Api.Common.Dto.Torrents.TorrentSourceKind;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal sealed class UpdateTorrentByIdHandler(TorrentService torrentService, TorrentSchedulerService scheduler)
{
    public async Task<UpdateTorrentByIdOutcome> TryUpdateTorrentByIdAsync(
        long id,
        long version,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outcome = await TryValidateAgainstStoredKindAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (outcome is not null)
            return outcome.Value;

        var dto = request.ToTorrentUpdateDto();

        var (result, currentVersion) = await torrentService
            .UpdateOneAsync(id, version, dto, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            TorrentMutationResult.Success => OnUpdated(id, dto.Cron),
            TorrentMutationResult.NotFound => OnNotFound(),
            TorrentMutationResult.VersionConflict =>
                OnConflict(TorrentErrorKeys.Version, EndpointMessages.TorrentModifiedConflict, currentVersion),
            TorrentMutationResult.NotUnique => // unreachable - we don't change anything unique in a torrent
                OnConflict(TorrentErrorKeys.Id, EndpointMessages.TorrentAlreadyExists, currentVersion),
            _ => throw new InvalidOperationException($"Unexpected {nameof(TorrentMutationResult)}: {result}")
        };
    }

    /// <returns>
    /// The outcome to answer with, or <see langword="null"/> to carry on with the update.
    /// </returns>
    /// <remarks>
    /// The request carries no source kind, so the stored torrent has to supply it. The read cannot
    /// go stale: a torrent's source is fixed once added, and ids are never reused. If the row is
    /// deleted in between, the update below answers 404 anyway.
    /// </remarks>
    private async Task<UpdateTorrentByIdOutcome?> TryValidateAgainstStoredKindAsync(
        long id,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        // Absent means "leave as is" or "clear to the default", which are not refused by any source kind
        // - and skipping the read is the point, since most updates carry neither setting.
        if (string.IsNullOrEmpty(request.MagnetRegexPattern) && string.IsNullOrEmpty(request.JsonValueFormat))
            return null;

        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return OnNotFound();

        var errors = TorrentSourceRules.Validate(
            (ApiSourceKind)torrent.SourceKind,
            request.MagnetRegexPattern,
            request.JsonValueFormat);

        return errors.Length is 0 ? null : new(UpdateTorrentByIdResult.InvalidRequest, null, errors);
    }

    private UpdateTorrentByIdOutcome OnUpdated(long id, string? cron)
    {
        if (cron is not null)
        {
            _ = scheduler.TryUnscheduleTorrentRefresh(id);
            if (cron.Length > 0)
                scheduler.ScheduleTorrentRefresh(id, cron);
        }

        return new(UpdateTorrentByIdResult.Updated, null, []);
    }

    private static UpdateTorrentByIdOutcome OnNotFound() =>
        new(UpdateTorrentByIdResult.NotFound, null, [new(TorrentErrorKeys.Id, [EndpointMessages.NoSuchTorrent])]);

    private static UpdateTorrentByIdOutcome OnConflict(string key, string message, long? currentVersion) =>
        new(UpdateTorrentByIdResult.Conflict, currentVersion, [new(key, [message])]);
}
