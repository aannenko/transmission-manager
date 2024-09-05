namespace TransmissionManager.Api.Database.Dto;

public sealed class TorrentAddDto
{
    public TorrentAddDto(
        string hashString,
        string name,
        string webPageUri,
        string downloadDir,
        string? magnetRegexPattern = null,
        string? cron = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashString);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(webPageUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadDir);

        if (magnetRegexPattern is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(magnetRegexPattern);

        if (cron is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cron);

        HashString = hashString;
        Name = name;
        WebPageUri = webPageUri;
        DownloadDir = downloadDir;
        MagnetRegexPattern = magnetRegexPattern;
        Cron = cron;
    }

    public string HashString { get; }

    public string Name { get; }

    public string WebPageUri { get; }

    public string DownloadDir { get; }

    public string? MagnetRegexPattern { get; }

    public string? Cron { get; }
}
