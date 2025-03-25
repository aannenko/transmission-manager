using System.Globalization;
using System.Net;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Utilities;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using Direction = TransmissionManager.Api.Actions.Torrents.FindPage.FindTorrentPageDirection;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal static class FindTorrentPageParametersExtensions
{
    private static readonly int _maxLongLength = long.MaxValue.ToString(CultureInfo.InvariantCulture).Length;
    private static readonly int _maxTorrentOrderItemLength = Enum.GetNames<TorrentOrder>().Max(i => i.Length);
    private static readonly int _maxDirectionItemLength = Enum.GetNames<Direction>().Max(i => i.Length);

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

        const string takeParamKey = $"?{nameof(take)}=";
        const string orderByParamKey = $"&{nameof(orderBy)}=";
        const string anchorIdParamKey = $"&{nameof(anchorId)}=";
        const string anchorValueParamKey = $"&{nameof(anchorValue)}=";
        const string directionParamKey = $"&{nameof(direction)}=";
        const string propertyStartsWithParamKey = $"&{nameof(propertyStartsWith)}=";
        const string cronExistsParamKey = $"&{nameof(cronExists)}=";

        var rentedArraySize = EndpointAddresses.TorrentsApi.Length + takeParamKey.Length + "1000".Length;

        if (orderBy is not TorrentOrder.Id)
            rentedArraySize += orderByParamKey.Length + _maxTorrentOrderItemLength;

        if (anchorId is not null)
            rentedArraySize += anchorIdParamKey.Length + _maxLongLength;

        if (!string.IsNullOrEmpty(anchorValue))
            rentedArraySize += anchorValueParamKey.Length +
                (anchorValue = WebUtility.UrlEncode(anchorValue)).Length;

        if (direction is not Direction.Forward)
            rentedArraySize += directionParamKey.Length + _maxDirectionItemLength;

        if (!string.IsNullOrEmpty(propertyStartsWith))
            rentedArraySize += propertyStartsWithParamKey.Length +
                (propertyStartsWith = WebUtility.UrlEncode(propertyStartsWith)).Length;

        if (cronExists is not null)
            rentedArraySize += cronExistsParamKey.Length + bool.FalseString.Length;

        using var builder = new ValueStringBuilder(rentedArraySize);

        builder.Append(EndpointAddresses.TorrentsApi);
        builder.Append(takeParamKey);
        builder.Append(take.ToString(CultureInfo.InvariantCulture));

        if (orderBy is not TorrentOrder.Id)
        {
            builder.Append(orderByParamKey);
            builder.Append(orderBy.ToString());
        }

        if (anchorId is not null)
        {
            builder.Append(anchorIdParamKey);
            builder.Append(anchorId.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(anchorValue))
        {
            builder.Append(anchorValueParamKey);
            builder.Append(anchorValue);
        }

        if (direction is not Direction.Forward)
        {
            builder.Append(directionParamKey);
            builder.Append(direction.ToString());
        }

        if (!string.IsNullOrEmpty(propertyStartsWith))
        {
            builder.Append(propertyStartsWithParamKey);
            builder.Append(propertyStartsWith);
        }

        if (cronExists is not null)
        {
            builder.Append(cronExistsParamKey);
            builder.Append(cronExists.Value.ToString());
        }

        return builder.ToString();
    }

    public static FindTorrentPageParameters? ToNextPageParameters(
        in this FindTorrentPageParameters parameters,
        Torrent[] currentPage)
    {
        return currentPage.Length is 0
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
        Torrent[] currentPage)
    {
        return currentPage.Length is 0
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
