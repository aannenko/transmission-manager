namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal enum UpdateTorrentByIdResult
{
    Updated,
    NotFound,
    VersionConflict,
    Exists,
}
