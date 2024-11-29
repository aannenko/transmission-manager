namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentPageDescriptor<TAfter>(
    TorrentOrder OrderBy,
    int Take,
    long AfterId,
    TAfter? After = default);
