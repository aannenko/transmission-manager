namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal interface IRefreshTorrentByIdHandler
{
    Task<RefreshTorrentByIdOutcome> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken);
}
