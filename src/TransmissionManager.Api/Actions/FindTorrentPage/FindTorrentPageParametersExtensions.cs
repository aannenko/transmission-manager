using System.Net;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

internal static class FindTorrentPageParametersExtensions
{
    public static TorrentFilter ToTorrentFilter(this FindTorrentPageParameters parameters)
    {
        return new(
            PropertyStartsWith: parameters.PropertyStartsWith,
            CronExists: parameters.CronExists);
    }

    public static string ToPathAndQueryString(this FindTorrentPageParameters parameters)
    {
        var (orderBy, take, afterId, after, propertyStartsWith, cronExists) = parameters;

        after = string.IsNullOrEmpty(after) ? null : WebUtility.UrlEncode(after);
        propertyStartsWith = string.IsNullOrEmpty(propertyStartsWith) ? null : WebUtility.UrlEncode(propertyStartsWith);

        var addOrderBy = orderBy is not TorrentOrder.Id;
        var addAfter = addOrderBy && after is not null;
        var addPropertyStartsWith = propertyStartsWith is not null;
        var addCronExists = cronExists is not null;

        return $"{EndpointAddresses.TorrentsApi}?{nameof(take)}={take}&{nameof(afterId)}={afterId}" +
            $"{(addAfter ? $"&{nameof(after)}={after}" : string.Empty)}" +
            $"{(addOrderBy ? $"&{nameof(orderBy)}={orderBy}" : string.Empty)}" +
            $"{(addPropertyStartsWith ? $"&{nameof(propertyStartsWith)}={propertyStartsWith}" : string.Empty)}" +
            $"{(addCronExists ? $"&{nameof(cronExists)}={cronExists}" : string.Empty)}";
    }

    public static FindTorrentPageParameters? ToNextPageParameters(
        this FindTorrentPageParameters parameters,
        IReadOnlyList<Torrent> currentPage)
    {
        return currentPage.Count < parameters.Take
            ? null
            : parameters with
            {
                AfterId = currentPage[^1].Id,
                After = parameters.OrderBy switch
                {
                    TorrentOrder.Id => null,
                    TorrentOrder.Name or TorrentOrder.NameDesc => currentPage[^1].Name,
                    TorrentOrder.WebPage or TorrentOrder.WebPageDesc => currentPage[^1].WebPageUri,
                    TorrentOrder.DownloadDir or TorrentOrder.DownloadDirDesc => currentPage[^1].DownloadDir,
                    _ => null,
                }
            };
    }
}
