namespace TransmissionManager.Api.Common.Constants;

/// <remarks>
/// <see cref="Errors"/> names the extension containing the error dictionary. The request-parameter
/// and request-processing regions contain keys used inside that dictionary. Framework-generated
/// body-field keys are Pascal-case and are not declared here.
/// </remarks>
public static class ProblemDetailsKeys
{
    #region Extension members

    public static readonly string CurrentVersion = "currentVersion";

    public static readonly string TransmissionResult = "transmissionResult";

    /// <remarks>
    /// The key ASP.NET Core reports validation failures under. Fill it with a dictionary
    /// with keys containing field names, and values being arrays of error messages.
    /// </remarks>
    public static readonly string Errors = "errors"; // keys below are put inside this dictionary

    #endregion

    #region Request parameters

    public static readonly string Id = "id";

    public static readonly string Version = "version";

    #endregion

    #region Request processing

    /// <summary>
    /// Transmission refused the request, could not be reached, or does not hold the torrent the
    /// request is about.
    /// </summary>
    public static readonly string Transmission = "transmission";

    /// <summary>
    /// The torrent operation as a whole when the local catalog rejects a source URI or hash and no
    /// individual request value identifies the conflicting torrent.
    /// </summary>
    public static readonly string Torrent = "torrent";

    /// <summary>
    /// The torrent's source as a whole - its address, its magnet pattern and its magnet format
    /// together.
    /// </summary>
    /// <remarks>
    /// A failed magnet search does not name which of the three is to blame, and mostly cannot: a
    /// pattern or a format that is malformed is refused before the source is ever read, so what
    /// reaches here is what only the message can explain - a page holding no magnet, a pattern
    /// matching nothing, a pointer addressing the wrong value.
    /// </remarks>
    public static readonly string TorrentSource = "torrentSource";

    #endregion
}
