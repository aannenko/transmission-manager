﻿using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
