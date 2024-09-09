using static TransmissionManager.Api.Dto.RefreshTorrentResult;

namespace TransmissionManager.Api.Dto;

public readonly record struct RefreshTorrentResult(ResultType Type, string? ErrorMessage)
{
    public enum ResultType
    {
        Success,
        NotFound,
        Error
    }
}
