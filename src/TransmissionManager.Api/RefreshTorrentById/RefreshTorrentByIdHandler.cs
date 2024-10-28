using TransmissionManager.Api.Common.Services;
using TransmissionManager.Database.Services;
using static TransmissionManager.Api.Common.Services.TransmissionClientWrapper;

namespace TransmissionManager.Api.RefreshTorrentById;

public sealed class RefreshTorrentByIdHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentQueryService queryService,
    TorrentCommandService commandService,
    TorrentNameUpdateService torrentNameUpdateService)
{
    public enum Result
    {
        TorrentRefreshed,
        NotFoundLocally,
        NotFoundInTransmission,
        Removed,
        DependencyFailed,
    }

    public readonly record struct Response(
        Result Result,
        TransmissionAddResult? TransmissionResult,
        string? Error);

    public async Task<Response> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string error = "Refresh of the torrent with id {0} has failed: '{1}'.";

        var torrent = await queryService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFoundLocally, null, string.Format(error, id, "No such torrent."));

        var (_, transmissionGetError) = await transmissionService
            .GetTorrentAsync(torrent.HashString, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetError is not null)
            return new(Result.NotFoundInTransmission, null, string.Format(error, id, transmissionGetError));

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(new(torrent.WebPageUri), torrent.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(Result.DependencyFailed, null, string.Format(error, id, getMagnetError));

        var (transmissionAddResult, transmissionAddTorrent, transmissionAddError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, torrent.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return new(Result.DependencyFailed, transmissionAddResult, string.Format(error, id, transmissionAddError));

        var isUpdated = await commandService
            .TryUpdateOneByIdAsync(torrent.Id, transmissionAddTorrent.ToTorrentUpdateDto(), cancellationToken)
            .ConfigureAwait(false);

        if (!isUpdated)
        {
            const string message = "The torrent was removed before it could be updated.";
            return new(Result.Removed, transmissionAddResult, string.Format(error, id, message));
        }

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = torrentNameUpdateService.UpdateTorrentNameAsync(id, transmissionAddTorrent.HashString);

        return new(Result.TorrentRefreshed, transmissionAddResult, null);
    }
}
