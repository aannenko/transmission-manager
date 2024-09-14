using static TransmissionManager.Api.RefreshTorrentById.Handlers.RefreshTorrentByIdResult;

namespace TransmissionManager.Api.RefreshTorrentById.Handlers;

public readonly record struct RefreshTorrentByIdResult(ResultType Type, string? ErrorMessage)
{
    public enum ResultType
    {
        Success,
        NotFound,
        Error
    }
}
