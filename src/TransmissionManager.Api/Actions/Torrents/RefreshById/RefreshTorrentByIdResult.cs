namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal enum RefreshTorrentByIdResult
{
    Refreshed,
    NotFoundLocally,
    NotFoundInTransmission,
    Removed,
    VersionConflict,
    Exists,
    DependencyFailed,
}
