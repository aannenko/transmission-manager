namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal readonly record struct UpdateTorrentByIdOutcome(
    UpdateTorrentByIdResult Result,
    long? CurrentVersion,
    KeyValuePair<string, string[]>[] Errors);
