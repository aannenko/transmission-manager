namespace TransmissionManager.Api.Actions;

internal static class EndpointMessages
{
    public const string NoSuchTorrent = "No such torrent.";

    public const string TorrentModifiedConflict = "The torrent has been modified by another client.";

    public const string TorrentRemovedConflict = "The torrent has been removed by another client.";

    public const string TorrentAlreadyExists = "Torrent already exists.";
}
