using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.FindTorrentPage;

public sealed record FindTorrentPageParameters(
    [property: Range(1, 1000)] int Take = 20,
    [property: Range(0, long.MaxValue, ErrorMessage = "Value must be greater than 0.")] long AfterId = 0,
    [property: RegularExpression("[0-9A-Fa-f]{40}")] string? HashString = null,
    [property: Url] Uri? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
