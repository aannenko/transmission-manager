using System.Globalization;
using System.Net;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Utilities;
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

        using var builder = new ValueStringBuilder(256);

        builder.Append($"{EndpointAddresses.TorrentsApi}?{nameof(take)}=");
        builder.Append(take.ToString(CultureInfo.InvariantCulture));

        if (orderBy is not TorrentOrder.Id)
        {
            builder.Append($"&{nameof(orderBy)}=");
            builder.Append(orderBy.ToString());
        }

        if (anchorId is not null)
        {
            builder.Append($"&{nameof(anchorId)}=");
            builder.Append(anchorId.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(anchorValue))
        {
            builder.Append($"&{nameof(anchorValue)}=");
            builder.Append(WebUtility.UrlEncode(anchorValue));
        }

        if (direction is not Direction.Forward)
        {
            builder.Append($"&{nameof(direction)}=");
            builder.Append(direction.ToString());
        }

        if (!string.IsNullOrEmpty(propertyStartsWith))
        {
            builder.Append($"&{nameof(propertyStartsWith)}=");
            builder.Append(WebUtility.UrlEncode(propertyStartsWith));
        }

        if (cronExists is not null)
        {
            builder.Append($"&{nameof(cronExists)}=");
            builder.Append(cronExists.Value.ToString());
        }

        return builder.ToString();
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
