using System.Net;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Utilities;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public static class GetTorrentPageParametersExtensions
{
    private static readonly int _maxTakeLength = (int)Math.Floor(Math.Log10(GetTorrentPageParameters.MaxTake)) + 1;
    private static readonly int _maxTorrentOrderItemLength = Enum.GetNames<Order>().Max(static i => i.Length);
    private static readonly int _maxDirectionItemLength = Enum.GetNames<Direction>().Max(static i => i.Length);

    public static string ToPathAndQueryString(in this GetTorrentPageParameters parameters)
    {
        var (orderBy, anchorId, anchorValue, take, direction, propertyStartsWith, cronExists) = parameters;

        const string takeParamKey = $"?{nameof(take)}=";
        const string orderByParamKey = $"&{nameof(orderBy)}=";
        const string anchorIdParamKey = $"&{nameof(anchorId)}=";
        const string anchorValueParamKey = $"&{nameof(anchorValue)}=";
        const string directionParamKey = $"&{nameof(direction)}=";
        const string propertyStartsWithParamKey = $"&{nameof(propertyStartsWith)}=";
        const string cronExistsParamKey = $"&{nameof(cronExists)}=";

        var rentedArraySize = EndpointAddresses.Torrents.Length + takeParamKey.Length + _maxTakeLength;

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

        builder.Append(EndpointAddresses.Torrents);
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
}
