using System.Net;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

internal static class FindTorrentPageParametersExtensions
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
            PropertyStartsWith: parameters.PropertyStartsWith,
            CronExists: parameters.CronExists);
    }

    public static string ToPathAndQueryString(this FindTorrentPageParameters parameters)
    {
        var (take, afterId, propertyStartsWith, cronExists) = parameters;
        propertyStartsWith = WebUtility.UrlEncode(propertyStartsWith);
        return $"{EndpointAddresses.TorrentsApi}?{nameof(take)}={take}&{nameof(afterId)}={afterId}" +
            $"{(string.IsNullOrEmpty(propertyStartsWith) ? string.Empty : $"&{nameof(propertyStartsWith)}={propertyStartsWith}")}" +
            $"{(cronExists is null ? string.Empty : $"&{nameof(cronExists)}={cronExists}")}";
    }

    public static FindTorrentPageParameters? ToNextPageParameters(
        this FindTorrentPageParameters parameters,
        IReadOnlyList<Torrent> currentPage)
    {
        ArgumentNullException.ThrowIfNull(currentPage);

        return currentPage.Count < parameters.Take ? null : parameters with { AfterId = currentPage[^1].Id };
    }
}
