namespace TransmissionManager.Api.Services.Transmission;

internal readonly record struct TransmissionRemoveTorrentOutcome(TransmissionRemoveTorrentResult Result, string? Error);
