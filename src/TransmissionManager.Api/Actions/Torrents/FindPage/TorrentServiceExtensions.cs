using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

internal static class TorrentServiceExtensions
{
    public static Task<Torrent[]> FindPageAsync(
        this TorrentService service,
        in FindTorrentPageParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var filter = GetFilter(parameters);
        if (parameters.OrderBy is FindTorrentPageOrder.RefreshDate or FindTorrentPageOrder.RefreshDateDesc &&
            DateTime.TryParse(parameters.AnchorValue, out var dateTimeAnchorValue))
        {
            var pageDescriptor = GetPageDescriptor(parameters, dateTimeAnchorValue);
            return service.FindPageAsync(pageDescriptor, filter, cancellationToken);
        }
        else
        {
            var pageDescriptor = GetPageDescriptor(parameters, parameters.AnchorValue);
            return service.FindPageAsync(pageDescriptor, filter, cancellationToken);
        }
    }

    public static TorrentPageDescriptor<TAnchor> GetPageDescriptor<TAnchor>(
        in FindTorrentPageParameters parameters,
        TAnchor? anchorValue)
    {
        return new TorrentPageDescriptor<TAnchor>(
            OrderBy: (TorrentOrder)parameters.OrderBy,
            AnchorId: parameters.AnchorId,
            AnchorValue: anchorValue,
            IsForwardPagination: parameters.Direction is FindTorrentPageDirection.Forward,
            Take: parameters.Take);
    }

    public static TorrentFilter GetFilter(in FindTorrentPageParameters parameters)
    {
        return new(
            PropertyStartsWith: parameters.PropertyStartsWith,
            CronExists: parameters.CronExists);
    }
}
