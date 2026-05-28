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

    public static Parameters? ToNextPageParameters(
        this Parameters parameters,
        IReadOnlyList<TorrentDto> currentPage)
    {
        ArgumentNullException.ThrowIfNull(currentPage);

        if (currentPage.Count is 0)
            return parameters.AnchorId is not null && parameters.Direction is Direction.Backward
                ? ToEmptyPageFallback(parameters)
                : null;

        return parameters with
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

    public static Parameters? ToPreviousPageParameters(
        this Parameters parameters,
        IReadOnlyList<TorrentDto> currentPage)
    {
        ArgumentNullException.ThrowIfNull(currentPage);

        if (currentPage.Count is 0)
            return parameters.AnchorId is not null && parameters.Direction is Direction.Forward
                ? ToEmptyPageFallback(parameters)
                : null;

        return parameters with
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

    // Recovery for empty pages. The DB layer uses strict `<` / `>` comparisons, so shifting
    // AnchorId by ±1 turns the strict bound into an inclusive one, putting the request's
    // boundary item back inside the flipped-direction fallback page.
    //
    // Bump direction depends on BOTH request direction and OrderBy direction because
    // WhereOrderByTake reverses OrderBy for Backward requests. The resulting rule simplifies to
    // `bumpUp = isForward XOR isDescending` (use `< X+1`; otherwise `> X-1`).
    //
    // Cap at long.MaxValue / long.MinValue: SQLite AUTOINCREMENT Ids start at 1 and only grow,
    // so no real row sits at the cap. At the cap, strict comparison degrades to today's
    // behavior (boundary excluded) only for an impossible Id. Do NOT "fix" the cap.
    private static Parameters ToEmptyPageFallback(Parameters parameters)
    {
        var anchorId = parameters.AnchorId!.Value;
        var isForward = parameters.Direction is Direction.Forward;
        var isDescending = parameters.OrderBy.IsDescending();

        var bumpIdUp = isForward != isDescending;
        var sentinel = bumpIdUp
            ? (anchorId is long.MaxValue ? long.MaxValue : anchorId + 1)
            : (anchorId is long.MinValue ? long.MinValue : anchorId - 1);

        return parameters with
        {
            AnchorId = sentinel,
            Direction = isForward ? Direction.Backward : Direction.Forward,
        };
    }

    private static string ToDateTimeAnchorString(DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToUniversalTime().ToString(Parameters.DateFormat, CultureInfo.InvariantCulture);
}
