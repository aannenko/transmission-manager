using System.Net;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using Direction = TransmissionManager.Api.Actions.Torrents.FindPage.FindTorrentPageDirection;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal static class FindTorrentPageParametersExtensions
{
    public static TorrentPageDescriptor<string> ToTorrentPageDescriptor(in this FindTorrentPageParameters parameters)
    {
        return new TorrentPageDescriptor<string>(
            OrderBy: parameters.OrderBy,
            AnchorId: parameters.AnchorId,
            AnchorValue: parameters.AnchorValue,
            IsForwardPagination: parameters.Direction is Direction.Forward,
            Take: parameters.Take);
    }

    public static TorrentFilter ToTorrentFilter(in this FindTorrentPageParameters parameters)
    {
        return new(
            PropertyStartsWith: parameters.PropertyStartsWith,
            CronExists: parameters.CronExists);
    }

    public static string ToPathAndQueryString(in this FindTorrentPageParameters parameters)
    {
        var (orderBy, anchorId, anchorValue, take, direction, propertyStartsWith, cronExists) = parameters;

        anchorValue = string.IsNullOrEmpty(anchorValue) ? null : WebUtility.UrlEncode(anchorValue);
        propertyStartsWith = string.IsNullOrEmpty(propertyStartsWith) ? null : WebUtility.UrlEncode(propertyStartsWith);

        var addOrderBy = orderBy is not TorrentOrder.Id;
        var addAnchorId = anchorId is not null;
        var addAnchorValue = orderBy is not TorrentOrder.Id and not TorrentOrder.IdDesc && anchorValue is not null;
        var addDirection = direction is not Direction.Forward;
        var addPropertyStartsWith = propertyStartsWith is not null;
        var addCronExists = cronExists is not null;

        return $"{EndpointAddresses.TorrentsApi}?{nameof(take)}={take}" +
            $"{(addOrderBy ? $"&{nameof(orderBy)}={orderBy}" : string.Empty)}" +
            $"{(addAnchorId ? $"&{nameof(anchorId)}={anchorId}" : string.Empty)}" +
            $"{(addAnchorValue ? $"&{nameof(anchorValue)}={anchorValue}" : string.Empty)}" +
            $"{(addDirection ? $"&{nameof(direction)}={direction}" : string.Empty)}" +
            $"{(addPropertyStartsWith ? $"&{nameof(propertyStartsWith)}={propertyStartsWith}" : string.Empty)}" +
            $"{(addCronExists ? $"&{nameof(cronExists)}={cronExists}" : string.Empty)}";
    }

    public static FindTorrentPageParameters? ToNextPageParameters(
        in this FindTorrentPageParameters parameters,
        IReadOnlyList<Torrent> currentPage)
    {
        return currentPage.Count is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[^1].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    TorrentOrder.Id or TorrentOrder.IdDesc => null,
                    TorrentOrder.Name or TorrentOrder.NameDesc => currentPage[^1].Name,
                    TorrentOrder.WebPage or TorrentOrder.WebPageDesc => currentPage[^1].WebPageUri,
                    TorrentOrder.DownloadDir or TorrentOrder.DownloadDirDesc => currentPage[^1].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Forward
            };
    }

    public static FindTorrentPageParameters? ToPreviousPageParameters(
        in this FindTorrentPageParameters parameters,
        IReadOnlyList<Torrent> currentPage)
    {
        return currentPage.Count is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[0].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    TorrentOrder.Id or TorrentOrder.IdDesc => null,
                    TorrentOrder.Name or TorrentOrder.NameDesc => currentPage[0].Name,
                    TorrentOrder.WebPage or TorrentOrder.WebPageDesc => currentPage[0].WebPageUri,
                    TorrentOrder.DownloadDir or TorrentOrder.DownloadDirDesc => currentPage[0].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Backward
            };
    }
}
