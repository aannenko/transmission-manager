namespace TransmissionManager.Database.Dto;

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
    public string? JsonValueFormat { get; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? Cron { get; }
}
