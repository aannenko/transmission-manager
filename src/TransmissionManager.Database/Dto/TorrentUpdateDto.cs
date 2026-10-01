namespace TransmissionManager.Database.Dto;

/// <summary>
/// A partial update to a torrent, carrying only the fields it changes.
/// </summary>
/// <remarks>
/// A <see langword="null"/> field is left alone, so an update carrying nothing at all is refused.
/// <see cref="MagnetRegexPattern"/>, <see cref="JsonValueFormat"/> and <see cref="Cron"/> take an
/// empty string to clear the stored value; the rest refuse empty or whitespace values.
/// </remarks>
public sealed class TorrentUpdateDto
{
    /// <summary>Initializes a partial torrent update.</summary>
    /// <param name="hashString">The replacement info hash.</param>
    /// <param name="refreshDate">The replacement refresh time.</param>
    /// <param name="name">The replacement torrent name.</param>
    /// <param name="downloadDir">The replacement Transmission download directory.</param>
    /// <param name="magnetRegexPattern">The replacement magnet-link regular expression.</param>
    /// <param name="jsonValueFormat">The replacement JSON value format.</param>
    /// <param name="cron">The replacement refresh schedule.</param>
    /// <exception cref="ArgumentException">
    /// No field is supplied, or <paramref name="hashString"/>, <paramref name="name"/> or
    /// <paramref name="downloadDir"/> is empty or whitespace.
    /// </exception>
    public TorrentUpdateDto(
        string? hashString = null,
        DateTime? refreshDate = null,
        string? name = null,
        string? downloadDir = null,
        string? magnetRegexPattern = null,
        string? jsonValueFormat = null,
        string? cron = null)
    {
        if (hashString is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(hashString);

        if (name is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (downloadDir is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(downloadDir);

        if (hashString is null && refreshDate is null && name is null && downloadDir is null
            && magnetRegexPattern is null && jsonValueFormat is null && cron is null)
        {
            throw new ArgumentException("At least one field must be provided.");
        }

        HashString = hashString;
        RefreshDate = refreshDate;
        Name = name;
        DownloadDir = downloadDir;
        MagnetRegexPattern = magnetRegexPattern;
        JsonValueFormat = jsonValueFormat;
        Cron = cron;
    }

    /// <summary>Gets the replacement info hash.</summary>
    /// <returns>The info hash, or <see langword="null"/> to leave it unchanged.</returns>
    public string? HashString { get; }

    /// <summary>Gets the replacement refresh time.</summary>
    /// <returns>The refresh time, or <see langword="null"/> to leave it unchanged.</returns>
    public DateTime? RefreshDate { get; }

    /// <summary>Gets the replacement torrent name.</summary>
    /// <returns>The name, or <see langword="null"/> to leave it unchanged.</returns>
    public string? Name { get; }

    /// <summary>Gets the replacement Transmission download directory.</summary>
    /// <returns>The download directory, or <see langword="null"/> to leave it unchanged.</returns>
    public string? DownloadDir { get; }

    /// <summary>Gets the replacement magnet-link regular expression.</summary>
    /// <returns>The regular expression, with an empty string clearing the stored value.</returns>
    public string? MagnetRegexPattern { get; }

    /// <summary>Gets the replacement JSON value format.</summary>
    /// <returns>The format, with an empty string clearing the stored value.</returns>
    public string? JsonValueFormat { get; }

    /// <summary>Gets the replacement refresh schedule.</summary>
    /// <returns>The cron expression, with an empty string clearing the stored value.</returns>
    public string? Cron { get; }
}
