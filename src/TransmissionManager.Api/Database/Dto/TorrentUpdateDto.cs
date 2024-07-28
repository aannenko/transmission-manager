namespace TransmissionManager.Api.Database.Dto;

public sealed class TorrentUpdateDto
{
    public long? TransmissionId { get; set; }

    public string? Name { get; set; }

    public string? DownloadDir { get; set; }

    // string.Empty -> null
    public string? MagnetRegexPattern { get; set; }

    // string.Empty -> null
    public string? Cron { get; set; }
}
