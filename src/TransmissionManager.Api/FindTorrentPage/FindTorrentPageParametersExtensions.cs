using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.FindTorrentPage;

public static class FindTorrentPageParametersExtensions
{
    public static PageDescriptor ToPageDescriptor(this FindTorrentPageParameters parameters)
    {
        return new(
            Take: parameters.Take,
            AfterId: parameters.AfterId);
    }

    public static TorrentFilter ToTorrentFilter(this FindTorrentPageParameters parameters)
    {
        return new(
            HashString: parameters.HashString,
            WebPageUri: parameters.WebPageUri?.AbsoluteUri,
            NameStartsWith: parameters.NameStartsWith,
            CronExists: parameters.CronExists);
    }

    public static string? GetNextPageAddress(this FindTorrentPageParameters parameters, Torrent[] currentPage)
    {
        if (currentPage.Length is 0 || parameters.WebPageUri is not null || parameters.HashString is not null)
            return null;

        var (take, afterId, hashString, webPageUri, nameStartsWith, cronExists) = parameters;

        return $"{EndpointAddresses.TorrentsApi}?{nameof(take)}={take}&{nameof(afterId)}={currentPage.Last().Id}" +
            $"{(string.IsNullOrEmpty(nameStartsWith) ? string.Empty : $"&{nameof(nameStartsWith)}={nameStartsWith}")}" +
            $"{(cronExists is null ? string.Empty : $"&{nameof(cronExists)}={cronExists}")}";
    }
}
