namespace TransmissionManager.TorrentSources.Dto;

/// <summary>
/// The outcome of a search for a magnet link and, when the search succeeded, the magnet link itself.
/// </summary>
/// <param name="Result">How the search ended.</param>
/// <param name="MagnetUri">Non-<see langword="null"/> if and only if <paramref name="Result"/> is
/// <see cref="MagnetSearchResult.Found"/>.</param>
/// <param name="Error">Why the search did not succeed, or <see langword="null"/> when it did.
/// Phrased so that a caller can surface it as-is.</param>
/// <remarks>
/// Construct through the static factories - they keep <paramref name="MagnetUri"/> and
/// <paramref name="Result"/> in agreement.
/// </remarks>
public readonly record struct MagnetSearchOutcome(MagnetSearchResult Result, Uri? MagnetUri, string? Error)
{
    /// <summary>
    /// Creates an outcome for a search that found a magnet link.
    /// </summary>
    /// <param name="magnetUri">The magnet link that was found.</param>
    /// <returns>An outcome with <see cref="MagnetSearchResult.Found"/> and no error.</returns>
    public static MagnetSearchOutcome Found(Uri magnetUri) => new(MagnetSearchResult.Found, magnetUri, null);

    /// <summary>
    /// Creates an outcome for a search that did not find a magnet link.
    /// </summary>
    /// <param name="result">How the search ended.</param>
    /// <param name="error">Why the search did not succeed.</param>
    /// <returns>An outcome with no magnet link.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="result"/> is <see cref="MagnetSearchResult.Found"/>, which carries
    /// a magnet link and so cannot be expressed as a failure.
    /// </exception>
    public static MagnetSearchOutcome Failure(MagnetSearchResult result, string error)
    {
        if (result is MagnetSearchResult.Found)
        {
            throw new ArgumentException(
                $"'{MagnetSearchResult.Found}' is not a failure; use {nameof(Found)} instead.",
                nameof(result));
        }

        return new(result, null, error);
    }
}
