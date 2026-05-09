using System.Globalization;
using System.Text;
using System.Text.Json;
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
}
