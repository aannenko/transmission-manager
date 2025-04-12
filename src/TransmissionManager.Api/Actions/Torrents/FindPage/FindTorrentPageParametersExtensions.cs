using System.Net;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Utilities;
using TransmissionManager.Database.Dto;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.FindTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.FindTorrentPageOrder;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class FindTorrentPageParametersExtensions
{
    private static readonly int _maxTakeLength = (int)Math.Floor(Math.Log10(FindTorrentPageParameters.MaxTake)) + 1;
    private static readonly int _maxTorrentOrderItemLength = Enum.GetNames<TorrentOrder>().Max(i => i.Length);
    private static readonly int _maxDirectionItemLength = Enum.GetNames<Direction>().Max(i => i.Length);

    public static TorrentPageDescriptor<string> ToTorrentPageDescriptor(in this FindTorrentPageParameters parameters)
    {
        return new TorrentPageDescriptor<string>(
            OrderBy: (TorrentOrder)parameters.OrderBy,
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

        var rentedArraySize = EndpointAddresses.TorrentsApi.Length + takeParamKey.Length + _maxTakeLength;

        if (orderBy is not Order.Id)
            rentedArraySize += orderByParamKey.Length + _maxTorrentOrderItemLength;

        if (anchorId is not null)
            rentedArraySize += anchorIdParamKey.Length + 19; // long.MaxValue.ToString().Length is 19

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
        builder.Append(take);

        if (orderBy is not Order.Id)
        {
            builder.Append(orderByParamKey);
            builder.Append(orderBy);
        }

        if (anchorId is not null)
        {
            builder.Append(anchorIdParamKey);
            builder.Append(anchorId.Value);
        }

        if (!string.IsNullOrEmpty(anchorValue))
        {
            builder.Append(anchorValueParamKey);
            builder.Append(anchorValue);
        }

        if (direction is not Direction.Forward)
        {
            builder.Append(directionParamKey);
            builder.Append(direction);
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
        TorrentDto[] currentPage)
    {
        return currentPage.Length is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[^1].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    Order.Id or Order.IdDesc => null,
                    Order.Name or Order.NameDesc => currentPage[^1].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[^1].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[^1].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Forward
            };
    }

    public static FindTorrentPageParameters? ToPreviousPageParameters(
        in this FindTorrentPageParameters parameters,
        TorrentDto[] currentPage)
    {
        return currentPage.Length is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[0].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    Order.Id or Order.IdDesc => null,
                    Order.Name or Order.NameDesc => currentPage[0].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[0].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[0].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Backward
            };
    }
}
