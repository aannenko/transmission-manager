namespace TransmissionManager.Database.Models;

public sealed class Torrent
{
    public required long Id { get; set; }

    public required string HashString { get; set; }

    public required DateTime HashStringDate { get; set; }

    public required string Name { get; set; }

#pragma warning disable CA1056 // URI-like properties should not be strings - filtering is easier with strings
    public required string WebPageUri { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    public required string DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }
}
