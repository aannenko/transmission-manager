namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal enum DeleteTorrentByIdResult
{
    Deleted,
    NotFound,
    Conflict,
    DependencyFailed
}
