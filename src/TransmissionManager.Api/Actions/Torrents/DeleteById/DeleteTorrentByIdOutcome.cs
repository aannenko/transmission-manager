namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal readonly record struct DeleteTorrentByIdOutcome(
    DeleteTorrentByIdResult Result,
    long? CurrentVersion,
    string? Error);
