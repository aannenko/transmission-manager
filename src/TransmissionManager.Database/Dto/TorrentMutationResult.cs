namespace TransmissionManager.Database.Dto;

public enum TorrentMutationResult
{
    /// <summary>
    /// The mutation was applied.
    /// </summary>
    /// <remarks>
    /// <see cref="TorrentMutationOutcome.CurrentVersion"/> carries the version the row holds
    /// afterwards: the incremented value for an update, the unchanged value for a delete.
    /// </remarks>
    Success,

    /// <summary>
    /// No row with the requested id exists.
    /// </summary>
    /// <remarks>
    /// Reported when the mutation matched no row and the disambiguating SELECT found none either,
    /// so it is best-effort under concurrent churn - see the <c>&lt;remarks&gt;</c> on
    /// <c>TorrentService</c>. <see cref="TorrentMutationOutcome.CurrentVersion"/> is
    /// <see langword="null"/>.
    /// </remarks>
    NotFound,

    /// <summary>
    /// The row exists, but its version differs from the one the caller supplied.
    /// </summary>
    /// <remarks>
    /// Retryable: <see cref="TorrentMutationOutcome.CurrentVersion"/> carries the row's current
    /// version, so a caller that re-reads the row can resubmit against it.
    /// </remarks>
    VersionConflict,

    /// <summary>
    /// A unique index rejected the write: another row already holds the same
    /// <c>HashString</c> or <c>SourceUri</c>.
    /// </summary>
    /// <remarks>
    /// Not retryable, unlike <see cref="VersionConflict"/>: the caller must change the conflicting
    /// value rather than resubmit. <see cref="TorrentMutationOutcome.CurrentVersion"/> is
    /// <see langword="null"/>.
    /// </remarks>
    NotUnique,
}
