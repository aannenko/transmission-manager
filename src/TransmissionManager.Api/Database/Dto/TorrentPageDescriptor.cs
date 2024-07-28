namespace TransmissionManager.Api.Database.Dto;

public readonly record struct TorrentPageDescriptor(
    int Take,
    long AfterId,
    string? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
