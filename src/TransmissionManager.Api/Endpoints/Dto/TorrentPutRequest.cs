namespace TransmissionManager.Api.Endpoints.Dto;

public sealed class TorrentPutRequest
{
    public long? TransmissionId { get; set; }

    public string? DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }
}
