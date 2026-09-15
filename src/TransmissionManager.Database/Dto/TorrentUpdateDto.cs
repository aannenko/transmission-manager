namespace TransmissionManager.Database.Dto;

/// <summary>
/// A partial update to a torrent, carrying only the fields it changes.
/// </summary>
/// <remarks>
/// A <see langword="null"/> field is left alone, so an update carrying nothing at all is refused.
/// <see cref="MagnetRegexPattern"/>, <see cref="JsonValueFormat"/> and <see cref="Cron"/> take an
/// empty string to clear the stored value; the rest refuse empty strings.
/// </remarks>
public sealed class TorrentUpdateDto
{
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

    public string? HashString { get; }

    public DateTime? RefreshDate { get; }

    public string? Name { get; }

    public string? DownloadDir { get; }

    public string? MagnetRegexPattern { get; }

    public string? JsonValueFormat { get; }

    public string? Cron { get; }
}
