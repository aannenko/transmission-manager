using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Database.Dto;

public sealed class TorrentAddDto
{
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceUri"/> is relative.
    /// </exception>
    public TorrentAddDto(
        string hashString,
        DateTime refreshDate,
        string name,
        Uri sourceUri,
        TorrentSourceKind sourceKind,
        string downloadDir,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? magnetRegexPattern = null,
        string? jsonValueFormat = null,
        string? cron = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashString);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadDir);

        if (!sourceUri.IsAbsoluteUri)
            throw new ArgumentException("The source URI must be absolute.", nameof(sourceUri));

        if (magnetRegexPattern is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(magnetRegexPattern);

        if (jsonValueFormat is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(jsonValueFormat);

        if (cron is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cron);

        HashString = hashString;
        RefreshDate = refreshDate;
        Name = name;
        SourceUri = sourceUri;
        SourceKind = sourceKind;
        DownloadDir = downloadDir;
        MagnetRegexPattern = magnetRegexPattern;
        JsonValueFormat = jsonValueFormat;
        Cron = cron;
    }

    public string HashString { get; }

    public DateTime RefreshDate { get; }

    public string Name { get; }

    public Uri SourceUri { get; }

    public TorrentSourceKind SourceKind { get; }

    public string DownloadDir { get; }

    public string? MagnetRegexPattern { get; }

    public string? JsonValueFormat { get; }

    public string? Cron { get; }
}
