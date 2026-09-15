namespace TransmissionManager.TorrentSources.JsonPointer;

internal enum JsonPointerResolution
{
    /// <summary>
    /// The pointer addressed a JSON string, which is returned alongside this result.
    /// </summary>
    Found,

    /// <summary>
    /// The document holds nothing at the pointer: a member is absent, an index is out of range, or
    /// the pointer continues past a value that has no children.
    /// </summary>
    NotFound,

    /// <summary>
    /// The pointer addressed a value that is not a JSON string, reported alongside its kind.
    /// </summary>
    /// <remarks>
    /// Includes JSON <c>null</c>: the pointer resolved, so calling it absent would misdescribe the
    /// document, and calling it transient would guess at what one read cannot know.
    /// </remarks>
    NotAString,
}
