namespace TransmissionManager.Api.Actions.Torrents;

internal enum RefreshTorrentByIdResult
{
    Refreshed,
    NotFoundLocally,
    NotFoundInTransmission,
    Removed,
    DependencyFailed,
}
