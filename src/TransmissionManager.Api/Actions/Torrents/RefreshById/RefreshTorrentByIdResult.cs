namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal enum RefreshTorrentByIdResult
{
    /// <summary>
    /// The torrent was successfully refreshed.
    /// </summary>
    /// <remarks>
    /// Covers a magnet Transmission added and one it already had alike: either way, the row was
    /// repointed if and only if Transmission reported a hash other than the stored one.
    /// </remarks>
    Refreshed,

    /// <summary>
    /// No torrent with the requested id exists in the local catalog.
    /// </summary>
    NotFoundLocally,

    /// <summary>
    /// Transmission answered the lookup without the torrent.
    /// </summary>
    /// <remarks>
    /// The two systems are independent, so absence is not compensated for automatically. A lookup
    /// request that fails is <see cref="DependencyFailed"/> instead.
    /// </remarks>
    NotFoundInTransmission,

    /// <summary>
    /// Another client deleted the torrent while it was being refreshed.
    /// </summary>
    Removed,

    /// <summary>
    /// Another client modified the torrent while it was being refreshed.
    /// </summary>
    /// <remarks>
    /// Retryable: the response carries the row's current version to resubmit against.
    /// </remarks>
    VersionConflict,

    /// <summary>
    /// The refreshed hash is already held by another torrent.
    /// </summary>
    /// <remarks>
    /// The new magnet is in Transmission and the previous torrent was left in place, because the
    /// local row could not be repointed at it. Surfaced as-is for the user to resolve.
    /// </remarks>
    Exists,

    /// <summary>
    /// The torrent's stored source configuration did not yield a magnet.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="DependencyFailed"/>: the source was never reached,
    /// or it answered and simply held no magnet at the selector, so the scheduled refresh
    /// keeps failing until the stored configuration is corrected - or, in the one case this
    /// misjudges, until the source starts serving the magnet again. See
    /// <c>MagnetSearchResultExtensions.IsUnprocessableSource</c>.
    /// </remarks>
    InvalidConfiguration,

    /// <summary>
    /// The torrent source could not be retrieved, Transmission could not be queried, or it did not
    /// accept the refreshed magnet.
    /// </summary>
    /// <remarks>
    /// Transient as far as this application can tell, so the scheduled refresh will try again.
    /// </remarks>
    DependencyFailed,
}
