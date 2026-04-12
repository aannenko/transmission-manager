using System.Globalization;
using System.Text;
using System.Text.Json;
using TransmissionManager.Api.Common.Dto.Torrents;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;
using Parameters = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageParameters;
using ParsedParams = TransmissionManager.Api.Actions.Torrents.GetPage.GetTorrentPageParsedParameters;

namespace TransmissionManager.Api.Actions.Torrents.GetPage;

internal static class GetTorrentPageParametersExtensions
{
    private static readonly string _orderByParamName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Parameters.OrderBy));

    private static readonly string _anchorValueParamName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Parameters.AnchorValue));

    private static readonly CompositeFormat _orderByAndAnchorErrorFormat = CompositeFormat.Parse(
        $"When {_orderByParamName} is '{{0}}', {_anchorValueParamName} must be '{{1}}'.");

    private static readonly CompositeFormat _dateTimeAnchorErrorFormat = CompositeFormat.Parse(
        $"When {_orderByParamName} is '{{0}}', {_anchorValueParamName} must match format '{{1}}'.");

    public static ParsedParams Parse(this Parameters parameters, out KeyValuePair<string, string[]>[]? errors)
    {
        errors = null;
        if (parameters.AnchorValue is null)
            return new(null);

        DateTime? dateTimeAnchor = null;

        if (parameters.OrderBy is Order.Id or Order.IdDesc)
        {
            errors =
            [
                new(_anchorValueParamName,
                    [string.Format(null, _orderByAndAnchorErrorFormat, parameters.OrderBy, "null")])
            ];
        }
        else if (parameters.OrderBy is Order.RefreshDate or Order.RefreshDateDesc)
        {
            if (TryParseDateTimeAnchorString(parameters.AnchorValue, out DateTime result))
            {
                dateTimeAnchor = result;
            }
            else
            {
                errors =
                [
                    new(_anchorValueParamName,
                        [string.Format(null, _dateTimeAnchorErrorFormat, parameters.OrderBy, Parameters.DateFormat)])
                ];
            }
        }

        return new(dateTimeAnchor);

        static bool TryParseDateTimeAnchorString(string? s, out DateTime result)
        {
            return DateTime.TryParseExact(
                s,
                Parameters.DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out result);
        }
    }

    public static Parameters? ToNextPageParameters(this Parameters parameters, TorrentDto[] currentPage)
    {
        return currentPage.Length is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[^1].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    Order.Id or Order.IdDesc => null,
                    Order.RefreshDate or Order.RefreshDateDesc => ToDateTimeAnchorString(currentPage[^1].RefreshDate),
                    Order.Name or Order.NameDesc => currentPage[^1].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[^1].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[^1].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Forward
            };
    }

    public static Parameters? ToPreviousPageParameters(this Parameters parameters, TorrentDto[] currentPage)
    {
        return currentPage.Length is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[0].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    Order.Id or Order.IdDesc => null,
                    Order.RefreshDate or Order.RefreshDateDesc => ToDateTimeAnchorString(currentPage[0].RefreshDate),
                    Order.Name or Order.NameDesc => currentPage[0].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[0].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[0].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Backward
            };
    }

    private static string ToDateTimeAnchorString(DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToUniversalTime().ToString(Parameters.DateFormat, CultureInfo.InvariantCulture);
}
