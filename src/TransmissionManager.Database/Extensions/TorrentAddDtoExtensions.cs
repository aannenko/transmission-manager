﻿using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Extensions;

public static class TorrentAddDtoExtensions
{
    public static Torrent ToTorrent(this TorrentAddDto dto)
    {
        return new()
        {
            Id = default,
            HashString = dto.HashString,
            Name = dto.Name,
            DownloadDir = dto.DownloadDir,
            WebPageUri = dto.WebPageUri,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            Cron = dto.Cron,
        };
    }
}
