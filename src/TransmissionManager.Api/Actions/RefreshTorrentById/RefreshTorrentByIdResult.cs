namespace TransmissionManager.Api.Actions.RefreshTorrentById;

internal enum RefreshTorrentByIdResult
{
    Refreshed,
    NotFoundLocally,
    NotFoundInTransmission,
    Removed,
    DependencyFailed,
}
