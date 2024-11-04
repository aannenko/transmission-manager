namespace TransmissionManager.Api.RefreshTorrentById;

public enum RefreshTorrentByIdResult
{
    TorrentRefreshed,
    NotFoundLocally,
    NotFoundInTransmission,
    Removed,
    DependencyFailed,
}
