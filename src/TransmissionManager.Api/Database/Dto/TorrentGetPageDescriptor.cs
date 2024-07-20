namespace TransmissionManager.Api.Database.Dto;

public readonly record struct TorrentGetPageDescriptor(
    int Take,
    long AfterId,
    string? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
