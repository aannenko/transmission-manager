namespace TransmissionManager.Database.Dto;

public enum TorrentUpdateResult
{
    Updated,
    NotFound,
    ConcurrencyConflict,
}
