﻿using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.AddTorrent;

public readonly record struct AddTorrentOutcome(
    AddTorrentResult Result,
    long? Id,
    TransmissionAddResult? TransmissionResult,
    string? Error);
