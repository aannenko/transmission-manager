using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Database.Dto;

public sealed class TorrentAddDto
{
    public TorrentAddDto(
        string hashString,
        DateTime hashStringDate,
        string name,
        Uri webPageUri,
        string downloadDir,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? magnetRegexPattern = null,
        string? cron = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashString);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(webPageUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadDir);

        if (magnetRegexPattern is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(magnetRegexPattern);

        if (cron is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cron);

        HashString = hashString;
        HashStringDate = hashStringDate;
        Name = name;
        WebPageUri = webPageUri;
        DownloadDir = downloadDir;
        MagnetRegexPattern = magnetRegexPattern;
        Cron = cron;
    }

    public string HashString { get; }

    public DateTime HashStringDate { get; }

    public string Name { get; }

    public Uri WebPageUri { get; }

    public string DownloadDir { get; }

    public string? MagnetRegexPattern { get; }

    public string? Cron { get; }
}
