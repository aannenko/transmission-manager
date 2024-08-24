namespace TransmissionManager.Api.Endpoints.Dto;

public sealed record TorrentFindPageParameters(
    int Take = 20,
    long AfterId = 0,
    string? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
