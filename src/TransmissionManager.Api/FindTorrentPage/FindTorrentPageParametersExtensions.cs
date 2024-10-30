using System.Net;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.FindTorrentPage;

public static class FindTorrentPageParametersExtensions
{
    public static PageDescriptor ToPageDescriptor(this FindTorrentPageParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return new(
            Take: parameters.Take,
            AfterId: parameters.AfterId);
    }

    public static TorrentFilter ToTorrentFilter(this FindTorrentPageParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return new(
            HashString: parameters.HashString,
            WebPageUri: parameters.WebPageUri?.AbsoluteUri,
            NameStartsWith: parameters.NameStartsWith,
            CronExists: parameters.CronExists);
    }

    public static string ToPathAndQueryString(this FindTorrentPageParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var (take, afterId, hashString, webPageUri, nameStartsWith, cronExists) = parameters;
        nameStartsWith = WebUtility.UrlEncode(nameStartsWith);
        return $"{EndpointAddresses.TorrentsApi}?{nameof(take)}={take}&{nameof(afterId)}={afterId}" +
            $"{(hashString is null ? string.Empty : $"&{nameof(hashString)}={hashString}")}" +
            $"{(webPageUri is null ? string.Empty : $"&{nameof(webPageUri)}={webPageUri}")}" +
            $"{(string.IsNullOrEmpty(nameStartsWith) ? string.Empty : $"&{nameof(nameStartsWith)}={nameStartsWith}")}" +
            $"{(cronExists is null ? string.Empty : $"&{nameof(cronExists)}={cronExists}")}";
    }

    public static FindTorrentPageParameters? ToNextPageParameters(
        this FindTorrentPageParameters parameters,
        Torrent[] currentPage)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(currentPage);

        if (currentPage.Length is 0 || parameters.WebPageUri is not null || parameters.HashString is not null)
            return null;

        return parameters with { AfterId = currentPage.Last().Id };
    }
}
