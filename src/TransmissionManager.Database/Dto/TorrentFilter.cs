namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentFilter(
    string? HashString = null,
    Uri? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
