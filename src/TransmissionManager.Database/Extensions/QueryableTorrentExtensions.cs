using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Extensions;

internal static class QueryableTorrentExtensions
{
    public static IQueryable<Torrent> Where<T>(
        this IQueryable<Torrent> query,
        TorrentOrder orderBy,
        long afterId,
        T after)
    {
        return (orderBy, after) switch
        {
            (TorrentOrder.Id, _) or (_, null or "") =>
                query.Where(torrent => torrent.Id > afterId),
            (TorrentOrder.Name, string name) =>
                query.Where(torrent =>
                    torrent.Name.CompareTo(name) > 0 ||
                    (torrent.Name.CompareTo(name) == 0 && torrent.Id > afterId)),
            (TorrentOrder.NameDesc, string name) =>
                query.Where(torrent =>
                    torrent.Name.CompareTo(name) < 0 ||
                    (torrent.Name.CompareTo(name) == 0 && torrent.Id > afterId)),
            (TorrentOrder.WebPage, string page) =>
                query.Where(torrent =>
                    torrent.WebPageUri.CompareTo(page) > 0 ||
                    (torrent.WebPageUri.CompareTo(page) == 0 && torrent.Id > afterId)),
            (TorrentOrder.WebPageDesc, string page) =>
                query.Where(torrent =>
                    torrent.WebPageUri.CompareTo(page) < 0 ||
                    (torrent.WebPageUri.CompareTo(page) == 0 && torrent.Id > afterId)),
            (TorrentOrder.DownloadDir, string dir) =>
                query.Where(torrent =>
                    torrent.DownloadDir.CompareTo(dir) > 0 ||
                    (torrent.DownloadDir.CompareTo(dir) == 0 && torrent.Id > afterId)),
            (TorrentOrder.DownloadDirDesc, string dir) =>
                query.Where(torrent =>
                    torrent.DownloadDir.CompareTo(dir) < 0 ||
                    (torrent.DownloadDir.CompareTo(dir) == 0 && torrent.Id > afterId)),
            _ => throw new ArgumentException(
                $"Incompatible arguments {nameof(orderBy)} ({orderBy}) and {nameof(after)} ({after}) were provided.",
                nameof(after))
        };
    }

    public static IOrderedQueryable<Torrent> OrderBy(this IQueryable<Torrent> query, TorrentOrder order)
    {
        var orderedQuery = order switch
        {
            TorrentOrder.Id => query.OrderBy(static torrent => torrent.Id),
            TorrentOrder.Name => query.OrderBy(static torrent => torrent.Name),
            TorrentOrder.NameDesc => query.OrderByDescending(static torrent => torrent.Name),
            TorrentOrder.WebPage => query.OrderBy(static torrent => torrent.WebPageUri),
            TorrentOrder.WebPageDesc => query.OrderByDescending(static torrent => torrent.WebPageUri),
            TorrentOrder.DownloadDir => query.OrderBy(static torrent => torrent.DownloadDir),
            TorrentOrder.DownloadDirDesc => query.OrderByDescending(static torrent => torrent.DownloadDir),
            _ => throw new ArgumentOutOfRangeException(nameof(order)),
        };

        return order is TorrentOrder.Id ? orderedQuery : orderedQuery.ThenBy(static torrent => torrent.Id);
    }
}
