namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal enum RefreshTorrentByIdResult
{
    /// <summary>
    /// The torrent was successfully refreshed.
    /// </summary>
    /// <remarks>
    /// Also covers a magnet that Transmission already had, in which case nothing changed locally.
    /// </remarks>
    Refreshed,

    /// <summary>
    /// No torrent with the requested id exists in the local catalog.
    /// </summary>
    NotFoundLocally,

    /// <summary>
    /// The torrent exists locally, but Transmission does not have it.
    /// </summary>
    /// <remarks>
    /// The two systems are independent, so this is a state the user resolves rather than an error
    /// to compensate for automatically.
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
    /// The torrent's stored source address or magnet regex cannot be used.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="DependencyFailed"/>: not transient, so the scheduled
    /// refresh will keep failing until the stored configuration is corrected.
    /// </remarks>
    InvalidConfiguration,

    /// <summary>
    /// The torrent web page or Transmission could not be reached, or yielded nothing usable.
    /// </summary>
    /// <remarks>
    /// Transient as far as this application can tell, so the scheduled refresh will try again.
    /// </remarks>
    DependencyFailed,
}
