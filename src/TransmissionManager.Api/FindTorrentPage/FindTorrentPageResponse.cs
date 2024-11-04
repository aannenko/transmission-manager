﻿using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.FindTorrentPage;

public readonly record struct FindTorrentPageResponse(IReadOnlyList<Torrent> Torrents, string? NextPageAddress);
