using Microsoft.EntityFrameworkCore;

namespace TransmissionManager.Database.Models;

[Index(nameof(HashString), IsUnique = true)]
[Index(nameof(WebPageUri), IsUnique = true)]
[Index(nameof(Name))]
public sealed class Torrent
{
    public required long Id { get; set; }

    public required string HashString { get; set; }

    public required string Name { get; set; }

    public required Uri WebPageUri { get; set; }

    public required string DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }
}
