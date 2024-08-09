using static TransmissionManager.Api.Composite.Dto.AddOrUpdateTorrentResult;

namespace TransmissionManager.Api.Composite.Dto;

public readonly record struct AddOrUpdateTorrentResult(ResultType Type, long Id, string? ErrorMessage)
{
    public enum ResultType
    {
        Add,
        Update,
        Error
    }
}
