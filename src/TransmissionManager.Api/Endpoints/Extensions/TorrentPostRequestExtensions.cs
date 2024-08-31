﻿using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Endpoints.Extensions;

public static class TorrentPostRequestExtensions
{
    public static TorrentAddDto ToTorrentAddDto(
        this TorrentPostRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            transmissionId: transmissionTorrent.Id,
            name: transmissionTorrent.Name,
            webPageUri: dto.WebPageUri,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }

    public static TorrentUpdateDto ToTorrentUpdateDto(
        this TorrentPostRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            transmissionId: transmissionTorrent.Id,
            name: transmissionTorrent.Name,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
