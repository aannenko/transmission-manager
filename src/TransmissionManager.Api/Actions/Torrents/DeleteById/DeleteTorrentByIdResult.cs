namespace TransmissionManager.Api.Actions.Torrents;

internal enum DeleteTorrentByIdResult
{
    Removed,
    NotFoundLocally,
    DependencyFailed
}
