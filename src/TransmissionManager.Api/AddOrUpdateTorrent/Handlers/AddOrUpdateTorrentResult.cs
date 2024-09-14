using static TransmissionManager.Api.AddOrUpdateTorrent.Handlers.AddOrUpdateTorrentResult;

namespace TransmissionManager.Api.AddOrUpdateTorrent.Handlers;

public readonly record struct AddOrUpdateTorrentResult(ResultType Type, long Id, string? ErrorMessage)
{
    public enum ResultType
    {
        Add,
        Update,
        Error
    }
}
