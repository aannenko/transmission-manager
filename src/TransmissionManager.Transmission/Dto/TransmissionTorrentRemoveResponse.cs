namespace TransmissionManager.Transmission.Dto;

/// <summary>Represents a Transmission <c>torrent-remove</c> response.</summary>
public sealed class TransmissionTorrentRemoveResponse : ITransmissionResponse
{
    /// <summary>Gets Transmission's RPC result.</summary>
    /// <returns>The result text.</returns>
    public required string Result { get; init; }

    /// <summary>Gets the optional request correlation tag.</summary>
    /// <returns>The correlation tag.</returns>
    public int? Tag { get; init; }
}
