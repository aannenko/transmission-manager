﻿using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal readonly record struct RefreshTorrentByIdResponse(TransmissionAddResult TransmissionResult);
