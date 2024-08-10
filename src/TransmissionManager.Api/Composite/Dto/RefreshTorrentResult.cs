using static TransmissionManager.Api.Composite.Dto.RefreshTorrentResult;

namespace TransmissionManager.Api.Composite.Dto;

public readonly record struct RefreshTorrentResult(ResultType Type, string? ErrorMessage)
{
    public enum ResultType
    {
        Success,
        NotFound,
        Error
    }
}
