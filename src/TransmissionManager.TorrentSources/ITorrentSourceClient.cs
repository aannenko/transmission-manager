namespace TransmissionManager.TorrentSources;

/// <summary>
/// Internal marker interface for clients that read a torrent source and report a magnet link.
/// </summary>
/// <remarks>
/// The clients it marks have different signatures and cannot share a common base type.
/// This interface lets a test discover every client and assert it is registered in DI,
/// so a new client cannot ship unregistered.
/// </remarks>
internal interface ITorrentSourceClient;
