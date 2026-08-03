namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal enum UpdateTorrentByIdResult
{
    /// <summary>
    /// The torrent was updated.
    /// </summary>
    /// <remarks>
    /// A changed cron expression is applied to the scheduler as part of this: an empty one cancels
    /// the scheduled refresh, a non-empty one replaces it.
    /// </remarks>
    Updated,

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
    /// Another torrent already holds the web page address the update asks for.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="VersionConflict"/>, resubmitting cannot help - the conflicting value has
    /// to change.
    /// </remarks>
    Exists,
}
