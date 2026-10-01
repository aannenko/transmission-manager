namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Defines which torrent data a delete request removes.</summary>
public enum DeleteTorrentByIdType
{
    /// <summary>Removes only the catalog entry.</summary>
    Local,
    /// <summary>Removes the catalog entry and the torrent from Transmission.</summary>
    LocalAndTransmission,
    /// <summary>Also removes the torrent's downloaded data.</summary>
    LocalAndTransmissionAndData,
}
