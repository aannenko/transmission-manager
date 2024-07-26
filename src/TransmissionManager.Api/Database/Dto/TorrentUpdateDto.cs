﻿namespace TransmissionManager.Api.Database.Dto;

public sealed class TorrentUpdateDto
{
    public long? TransmissionId { get; set; }

    public string? Name { get; set; }

    public string? DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }
}
