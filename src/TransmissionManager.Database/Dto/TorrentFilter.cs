namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentFilter(
    string? HashString = null,
    string? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
