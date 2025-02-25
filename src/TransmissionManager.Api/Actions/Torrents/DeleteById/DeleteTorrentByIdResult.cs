namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal enum DeleteTorrentByIdResult
{
    Removed,
    NotFoundLocally,
    DependencyFailed
}
