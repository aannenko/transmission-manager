using System.Globalization;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

internal static class TorrentServiceExtensions
{
    public static Task<Torrent[]> GetPageAsync(
        this TorrentService service,
        in GetTorrentPageParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var filter = GetFilter(parameters);
        if (parameters.OrderBy is GetTorrentPageOrder.RefreshDate or GetTorrentPageOrder.RefreshDateDesc &&
            TryParseDateTimeAnchorString(parameters.AnchorValue, out var dateTimeAnchorValue))
        {
            var pageDescriptor = GetPageDescriptor(parameters, dateTimeAnchorValue);
            return service.GetPageAsync(pageDescriptor, filter, cancellationToken);
        }
        else
        {
            var pageDescriptor = GetPageDescriptor(parameters, parameters.AnchorValue);
            return service.GetPageAsync(pageDescriptor, filter, cancellationToken);
        }

        static bool TryParseDateTimeAnchorString(string? s, out DateTime result)
        {
            return DateTime.TryParseExact(
                s,
                GetTorrentPageParameters.DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out result);
        }
    }

    private static TorrentPageDescriptor<TAnchor> GetPageDescriptor<TAnchor>(
        in GetTorrentPageParameters parameters,
        TAnchor? anchorValue)
    {
        return new(
            OrderBy: (TorrentOrder)parameters.OrderBy,
            AnchorId: parameters.AnchorId,
            AnchorValue: anchorValue,
            IsForwardPagination: parameters.Direction is GetTorrentPageDirection.Forward,
            Take: parameters.Take);
    }

    private static TorrentFilter GetFilter(in GetTorrentPageParameters parameters)
    {
        return new(
            PropertyStartsWith: parameters.PropertyStartsWith,
            CronExists: parameters.CronExists);
    }
}
