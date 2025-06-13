namespace TransmissionManager.Database.Dto;

public sealed class TorrentUpdateDto
{
    public TorrentUpdateDto(
        string? hashString = null,
        DateTime? refreshDate = null,
        string? name = null,
        string? downloadDir = null,
        string? magnetRegexPattern = null,
        string? cron = null)
    {
        if (hashString is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(hashString));

        if (name is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (downloadDir is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(downloadDir);

        HashString = hashString;
        RefreshDate = refreshDate;
        Name = name;
        DownloadDir = downloadDir;
        MagnetRegexPattern = magnetRegexPattern;
        Cron = cron;
    }

    // null is ignored
    public string? HashString { get; }

    // null is ignored
    public DateTime? RefreshDate { get; }

    // null is ignored
    public string? Name { get; }

    // null is ignored
    public string? DownloadDir { get; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? MagnetRegexPattern { get; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? Cron { get; }
}
