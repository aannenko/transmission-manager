using static TransmissionManager.Api.Dto.AddOrUpdateTorrentResult;

namespace TransmissionManager.Api.Dto;

public readonly record struct AddOrUpdateTorrentResult(ResultType Type, long Id, string? ErrorMessage)
{
    public enum ResultType
    {
        Add,
        Update,
        Error
    }
}
