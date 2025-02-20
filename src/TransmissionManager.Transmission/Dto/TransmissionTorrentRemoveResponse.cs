namespace TransmissionManager.Transmission.Dto;

public sealed class TransmissionTorrentRemoveResponse : ITransmissionResponse
{
    public required string Result { get; init; }

    public int? Tag { get; init; }
}
