namespace TransmissionManager.Api.Actions.RefreshTorrentById;

public enum RefreshTorrentByIdResult
{
    TorrentRefreshed,
    NotFoundLocally,
    NotFoundInTransmission,
    Removed,
    DependencyFailed,
}
