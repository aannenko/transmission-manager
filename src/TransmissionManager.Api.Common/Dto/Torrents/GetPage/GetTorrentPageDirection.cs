namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Defines the direction of keyset pagination.</summary>
public enum GetTorrentPageDirection
{
    /// <summary>Moves after the supplied anchor.</summary>
    Forward,
    /// <summary>Moves before the supplied anchor.</summary>
    Backward
}
