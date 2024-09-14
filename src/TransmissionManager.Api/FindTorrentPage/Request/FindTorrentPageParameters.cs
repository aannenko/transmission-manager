namespace TransmissionManager.Api.FindTorrentPage.Request;

public sealed record FindTorrentPageParameters(
    int Take = 20,
    long AfterId = 0,
    string? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
