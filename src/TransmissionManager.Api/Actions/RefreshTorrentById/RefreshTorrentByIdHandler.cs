using System.Text;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.Common.TorrentWebPage;
using TransmissionManager.Api.Common.Transmission;
using TransmissionManager.Database.Services;
using Result = TransmissionManager.Api.Actions.RefreshTorrentById.RefreshTorrentByIdResult;

namespace TransmissionManager.Api.Actions.RefreshTorrentById;

internal sealed class RefreshTorrentByIdHandler(
    TorrentWebPageClientWrapper torrentWebPageService,
    TransmissionClientWrapper transmissionService,
    TorrentService torrentService,
    TorrentNameUpdateService torrentNameUpdateService)
{
    private static readonly CompositeFormat _error =
        CompositeFormat.Parse("Refresh of the torrent with id {0} has failed: '{1}'.");

    public async Task<RefreshTorrentByIdOutcome> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken)
    {
        var torrent = await torrentService.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (torrent is null)
            return new(Result.NotFoundLocally, null, GetError(id, "No such torrent."));

        var (_, transmissionGetError) = await transmissionService
            .GetTorrentAsync(torrent.HashString, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetError is not null)
            return new(Result.NotFoundInTransmission, null, GetError(id, transmissionGetError));

        var (magnetUri, getMagnetError) = await torrentWebPageService
            .GetMagnetUriAsync(new(torrent.WebPageUri), torrent.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return new(Result.DependencyFailed, null, GetError(id, getMagnetError));

        var (transmissionAddResult, transmissionAddTorrent, transmissionAddError) = await transmissionService
            .AddTorrentUsingMagnetAsync(magnetUri, torrent.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionAddTorrent is null)
            return new(Result.DependencyFailed, transmissionAddResult, GetError(id, transmissionAddError));

        var isUpdated = await torrentService
            .TryUpdateOneByIdAsync(torrent.Id, transmissionAddTorrent.ToTorrentUpdateDto(), cancellationToken)
            .ConfigureAwait(false);

        if (!isUpdated)
        {
            const string message = "The torrent was removed before it could be updated.";
            return new(Result.Removed, transmissionAddResult, GetError(id, message));
        }

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = torrentNameUpdateService.UpdateTorrentNameAsync(id, transmissionAddTorrent.HashString);

        return new(Result.TorrentRefreshed, transmissionAddResult, null);
    }

    private static string GetError(long id, string? message) =>
        string.Format(null, _error, id, message);
}
