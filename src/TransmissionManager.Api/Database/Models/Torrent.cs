using Microsoft.EntityFrameworkCore;

namespace TransmissionManager.Api.Database.Models;

[Index(nameof(TransmissionId), IsUnique = true)]
[Index(nameof(WebPageUri), IsUnique = true)]
[Index(nameof(Name))]
public sealed class Torrent
{
    public required long Id { get; set; }

    public required long TransmissionId { get; set; }

    public required string Name { get; set; }

    public required string WebPageUri { get; set; }

    public required string DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }
}
