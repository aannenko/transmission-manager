namespace TransmissionManager.Database.Dto;

/// <summary>Reports the result of a concurrency-checked torrent mutation.</summary>
/// <param name="Result">How the mutation ended.</param>
/// <param name="CurrentVersion">The resulting or conflicting version when the result carries one.</param>
public readonly record struct TorrentMutationOutcome(TorrentMutationResult Result, long? CurrentVersion);
