namespace TransmissionManager.Api.Endpoints.Dto;

public sealed class TorrentPostRequest
{
    public required string DownloadDir { get; init; }

    public required string WebPageUri { get; init; }

    public string? MagnetRegexPattern { get; init; }

    public string? Cron { get; init; }
}
