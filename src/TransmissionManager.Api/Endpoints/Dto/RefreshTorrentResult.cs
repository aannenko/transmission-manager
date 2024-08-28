using static TransmissionManager.Api.Endpoints.Dto.RefreshTorrentResult;

namespace TransmissionManager.Api.Endpoints.Dto;

public readonly record struct RefreshTorrentResult(ResultType Type, string? ErrorMessage)
{
    public enum ResultType
    {
        Success,
        NotFound,
        Error
    }
}
