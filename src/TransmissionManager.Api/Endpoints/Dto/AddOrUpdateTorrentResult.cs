using static TransmissionManager.Api.Endpoints.Dto.AddOrUpdateTorrentResult;

namespace TransmissionManager.Api.Endpoints.Dto;

public readonly record struct AddOrUpdateTorrentResult(ResultType Type, long Id, string? ErrorMessage)
{
    public enum ResultType
    {
        Add,
        Update,
        Error
    }
}
