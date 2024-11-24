namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentFilter(string? PropertyStartsWith = null, bool? CronExists = null);
