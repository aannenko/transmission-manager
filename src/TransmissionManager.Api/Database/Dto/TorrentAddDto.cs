namespace TransmissionManager.Api.Database.Dto;

public sealed class TorrentAddDto
{
    public required long TransmissionId { get; set; }

    public required string Name { get; set; }

    public required string WebPageUri { get; set; }
    
    public required string DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }
}
