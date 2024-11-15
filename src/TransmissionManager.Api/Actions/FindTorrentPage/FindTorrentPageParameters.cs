using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

internal readonly record struct FindTorrentPageParameters(
    [property: Range(1, 1000)] int Take = 20,
    long AfterId = 0,
    [property: RegularExpression("[0-9A-Fa-f]{40}")] string? HashString = null,
    Uri? WebPageUri = null,
    string? NameStartsWith = null,
    bool? CronExists = null);
