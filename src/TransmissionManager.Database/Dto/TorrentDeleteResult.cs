namespace TransmissionManager.Database.Dto;

public enum TorrentDeleteResult
{
    Deleted,
    NotFound,
    ConcurrencyConflict,
}
