namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal readonly record struct DeleteTorrentByIdOutcome(
    DeleteTorrentByIdResult Result,
    long? CurrentVersion,
    KeyValuePair<string, string[]>[] Errors);
