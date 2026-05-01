namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentMutationOutcome(TorrentMutationResult Result, long? CurrentVersion);
