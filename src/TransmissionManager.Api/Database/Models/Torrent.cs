using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Utilities;

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

    [RegularExpression(AppRegex.FindMagnet)]
    public string? MagnetRegexPattern { get; set; }

    [RegularExpression(AppRegex.IsCron, MatchTimeoutInMilliseconds = 50)]
    public string? Cron { get; set; }
}
