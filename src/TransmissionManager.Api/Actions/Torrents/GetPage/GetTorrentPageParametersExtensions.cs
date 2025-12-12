using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;
using Parameters = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageParameters;

namespace TransmissionManager.Api.Common.Dto.Torrents;

internal static partial class GetTorrentPageParametersExtensions
{
    private const string _dateFormat = "yyyyMMddHHmmssfffffffZ";
    private const string _dateRegexPattern =
        @"^\d{4}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])([01]\d|2[0-3])[0-5]\d[0-5]\d\d{7}Z$";

    private static readonly string _orderByParamName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Parameters.OrderBy));

    private static readonly string _anchorValueParamName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Parameters.AnchorValue));

    private static readonly CompositeFormat _orderByAndAnchorValueErrorFormat = CompositeFormat.Parse(
        $"When {_orderByParamName} is '{{0}}', {_anchorValueParamName} must be '{{1}}'.");

    private static readonly CompositeFormat _dateTimeAnchorValueErrorFormat = CompositeFormat.Parse(
        $"When {_orderByParamName} is '{{0}}', {_anchorValueParamName} must match format '{{1}}'.");

    extension(in Parameters parameters)
    {
        public static string DateFormat => _dateFormat;

        public KeyValuePair<string, string[]>[]? Validate()
        {
            if (parameters.AnchorValue is null)
                return null;

            if (parameters.OrderBy is Order.Id or Order.IdDesc)
            {
                return
                [
                    new(_anchorValueParamName,
                        [string.Format(null, _orderByAndAnchorValueErrorFormat, parameters.OrderBy, "null")])
                ];
            }
            else if (parameters.OrderBy is Order.RefreshDate or Order.RefreshDateDesc
                && !DateRegex().IsMatch(parameters.AnchorValue))
            {
                return
                [
                    new(_anchorValueParamName,
                        [string.Format(null, _dateTimeAnchorValueErrorFormat, parameters.OrderBy, _dateFormat)])
                ];
            }

            return null;
        }

        public Parameters? ToNextPageParameters(TorrentDto[] currentPage)
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

        public Parameters? ToPreviousPageParameters(TorrentDto[] currentPage)
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
    }

    private static string ToDateTimeAnchorString(DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToUniversalTime().ToString(_dateFormat, CultureInfo.InvariantCulture);

    [GeneratedRegex(_dateRegexPattern, RegexOptions.ExplicitCapture, 50)]
    private static partial Regex DateRegex();
}
