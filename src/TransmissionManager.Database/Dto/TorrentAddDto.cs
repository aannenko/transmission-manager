using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Database.Dto;

/// <summary>Supplies the values required to insert a torrent.</summary>
public sealed class TorrentAddDto
{
    /// <summary>Initializes a torrent to insert.</summary>
    /// <param name="hashString">The torrent info hash.</param>
    /// <param name="refreshDate">The last refresh time.</param>
    /// <param name="name">The torrent name.</param>
    /// <param name="sourceUri">The absolute torrent source URI.</param>
    /// <param name="sourceKind">The torrent source kind.</param>
    /// <param name="downloadDir">The Transmission download directory.</param>
    /// <param name="magnetRegexPattern">The optional regular expression used to find a magnet link.</param>
    /// <param name="jsonValueFormat">The optional format used to turn a JSON value into a magnet link.</param>
    /// <param name="cron">The optional refresh schedule.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="hashString"/>, <paramref name="name"/>, <paramref name="sourceUri"/> or
    /// <paramref name="downloadDir"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A required string is empty or whitespace, an optional string is present but empty or whitespace,
    /// or <paramref name="sourceUri"/> is relative.
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

    /// <summary>Gets the torrent info hash.</summary>
    /// <returns>The info hash.</returns>
    public string HashString { get; }

    /// <summary>Gets the last refresh time.</summary>
    /// <returns>The refresh time.</returns>
    public DateTime RefreshDate { get; }

    /// <summary>Gets the name.</summary>
    /// <returns>The name.</returns>
    public string Name { get; }

    /// <summary>Gets the address from which to find the magnet link.</summary>
    /// <returns>The absolute source URI.</returns>
    public Uri SourceUri { get; }

    /// <summary>Gets how the source is interpreted.</summary>
    /// <returns>The source kind.</returns>
    public TorrentSourceKind SourceKind { get; }

    /// <summary>Gets the target directory in Transmission.</summary>
    /// <returns>The download directory.</returns>
    public string DownloadDir { get; }

    /// <summary>Gets the optional regular expression used to find a magnet link.</summary>
    /// <returns>The magnet-link regular expression.</returns>
    public string? MagnetRegexPattern { get; }

    /// <summary>Gets the optional format used to turn a JSON value into a magnet link.</summary>
    /// <returns>The JSON value format.</returns>
    public string? JsonValueFormat { get; }

    /// <summary>Gets the optional refresh schedule.</summary>
    /// <returns>The five-field cron expression.</returns>
    public string? Cron { get; }
}
