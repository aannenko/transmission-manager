namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal enum DeleteTorrentByIdResult
{
    /// <summary>
    /// The torrent was deleted either only locally, or from Transmission too if this was asked for.
    /// </summary>
    /// <remarks>
    /// Any scheduled refresh for the torrent is cancelled as part of this.
    /// </remarks>
    Deleted,

    /// <summary>
    /// No torrent with the requested id exists.
    /// </summary>
    NotFound,

    /// <summary>
    /// Another client modified the torrent since the caller read it.
    /// </summary>
    /// <remarks>
    /// Retryable: the response carries the row's current version to resubmit against.
    /// </remarks>
    VersionConflict,

    /// <summary>
    /// Transmission could not be reached, or refused to remove the torrent.
    /// </summary>
    /// <remarks>
    /// Removal from Transmission is attempted first, so nothing was deleted locally and the whole
    /// request may be retried. Cannot occur for a local-only deletion, which never contacts
    /// Transmission.
    /// </remarks>
    DependencyFailed
}
