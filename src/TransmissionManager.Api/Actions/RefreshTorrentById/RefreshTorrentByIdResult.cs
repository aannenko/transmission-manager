namespace TransmissionManager.Api.Actions.RefreshTorrentById;

internal enum RefreshTorrentByIdResult
{
    TorrentRefreshed,
    NotFoundLocally,
    NotFoundInTransmission,
    Removed,
    DependencyFailed,
}
