namespace TransmissionManager.Api.Actions.DeleteTorrentById;

internal enum DeleteTorrentByIdResult
{
    Removed,
    NotFoundLocally,
    DependencyFailed
}
