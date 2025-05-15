using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace System.Linq;

internal static class QueryableTorrentExtensions
{
    public static IQueryable<Torrent> WhereOrderByTake<T>(
        this IQueryable<Torrent> query,
        in TorrentPageDescriptor<T> page)
    {
        var isWhereRequired = page.AnchorId is not null || page.AnchorValue is not null;

        if (page.IsForwardPagination)
        {
            if (isWhereRequired)
                query = query.Where(page.OrderBy, page.AnchorId, page.AnchorValue);

            return query.OrderBy(page.OrderBy).Take(page.Take);
        }
        else
        {
            var reverseOrder = page.OrderBy.Reverse();

            if (isWhereRequired)
                query = query.Where(reverseOrder, page.AnchorId, page.AnchorValue);

            return query.OrderBy(reverseOrder).Take(page.Take).OrderBy(page.OrderBy);
        }
    }

    private static IQueryable<Torrent> Where<T>(
        this IQueryable<Torrent> query,
        TorrentOrder orderBy,
        long? anchorId,
        T? anchorValue)
    {
        if (anchorValue is null)
            orderBy = orderBy.IsDescending() ? TorrentOrder.IdDesc : TorrentOrder.Id;

        anchorId ??= orderBy.IsDescending() ? long.MaxValue : 0;

        return (orderBy, anchorValue) switch
        {
            (TorrentOrder.Id, _) =>
                query.Where(torrent => torrent.Id > anchorId),
            (TorrentOrder.IdDesc, _) =>
                query.Where(torrent => torrent.Id < anchorId),
            (TorrentOrder.Name, string name) =>
                query.Where(torrent =>
                    torrent.Name.CompareTo(name) > 0 ||
                    (torrent.Name.CompareTo(name) == 0 && torrent.Id > anchorId)),
            (TorrentOrder.NameDesc, string name) =>
                query.Where(torrent =>
                    torrent.Name.CompareTo(name) < 0 ||
                    (torrent.Name.CompareTo(name) == 0 && torrent.Id < anchorId)),
            (TorrentOrder.WebPage, string webPage) =>
                query.Where(torrent =>
                    torrent.WebPageUri.CompareTo(webPage) > 0 ||
                    (torrent.WebPageUri.CompareTo(webPage) == 0 && torrent.Id > anchorId)),
            (TorrentOrder.WebPageDesc, string webPage) =>
                query.Where(torrent =>
                    torrent.WebPageUri.CompareTo(webPage) < 0 ||
                    (torrent.WebPageUri.CompareTo(webPage) == 0 && torrent.Id < anchorId)),
            (TorrentOrder.DownloadDir, string downloadDir) =>
                query.Where(torrent =>
                    torrent.DownloadDir.CompareTo(downloadDir) > 0 ||
                    (torrent.DownloadDir.CompareTo(downloadDir) == 0 && torrent.Id > anchorId)),
            (TorrentOrder.DownloadDirDesc, string downloadDir) =>
                query.Where(torrent =>
                    torrent.DownloadDir.CompareTo(downloadDir) < 0 ||
                    (torrent.DownloadDir.CompareTo(downloadDir) == 0 && torrent.Id < anchorId)),
            _ => throw new ArgumentException( // Should never happen due to validation in the TorrentPageDescriptor
                string.Format(null, TorrentPageDescriptor<T>.OrderByAndAnchorValueErrorFormat, orderBy, anchorId),
                nameof(anchorValue))
        };
    }

    private static IOrderedQueryable<Torrent> OrderBy(this IQueryable<Torrent> query, TorrentOrder order)
    {
        var orderedQuery = order switch
        {
            TorrentOrder.Id => query.OrderBy(static torrent => torrent.Id),
            TorrentOrder.IdDesc => query.OrderByDescending(static torrent => torrent.Id),
            TorrentOrder.Name => query.OrderBy(static torrent => torrent.Name),
            TorrentOrder.NameDesc => query.OrderByDescending(static torrent => torrent.Name),
            TorrentOrder.WebPage => query.OrderBy(static torrent => torrent.WebPageUri),
            TorrentOrder.WebPageDesc => query.OrderByDescending(static torrent => torrent.WebPageUri),
            TorrentOrder.DownloadDir => query.OrderBy(static torrent => torrent.DownloadDir),
            TorrentOrder.DownloadDirDesc => query.OrderByDescending(static torrent => torrent.DownloadDir),
            _ => throw new ArgumentOutOfRangeException(nameof(order)),
        };

        if (order is TorrentOrder.Id or TorrentOrder.IdDesc)
            return orderedQuery;

        return order.IsDescending()
            ? orderedQuery.ThenByDescending(static torrent => torrent.Id)
            : orderedQuery.ThenBy(static torrent => torrent.Id);
    }
}
