namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentFilter(
    string? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
