namespace TransmissionManager.Database.Dto;

internal static class TorrentOrderExtensions
{
    public static bool IsDescending(this TorrentOrder orderBy)
    {
        return orderBy is
            TorrentOrder.IdDesc or
            TorrentOrder.NameDesc or
            TorrentOrder.WebPageDesc or
            TorrentOrder.DownloadDirDesc;
    }

    public static TorrentOrder Reverse(this TorrentOrder orderBy)
    {
        return orderBy switch
        {
            TorrentOrder.Id => TorrentOrder.IdDesc,
            TorrentOrder.IdDesc => TorrentOrder.Id,
            TorrentOrder.Name => TorrentOrder.NameDesc,
            TorrentOrder.NameDesc => TorrentOrder.Name,
            TorrentOrder.WebPage => TorrentOrder.WebPageDesc,
            TorrentOrder.WebPageDesc => TorrentOrder.WebPage,
            TorrentOrder.DownloadDir => TorrentOrder.DownloadDirDesc,
            TorrentOrder.DownloadDirDesc => TorrentOrder.DownloadDir,
            _ => throw new ArgumentOutOfRangeException(nameof(orderBy))
        };
    }

    public static bool IsCompatibleWith<TAnchor>(this TorrentOrder orderBy, TAnchor? anchorValue)
    {
        return orderBy switch
        {
            TorrentOrder.Id or TorrentOrder.IdDesc => anchorValue is null,
            TorrentOrder.Name or TorrentOrder.NameDesc => anchorValue is null or string,
            TorrentOrder.WebPage or TorrentOrder.WebPageDesc => anchorValue is null or string,
            TorrentOrder.DownloadDir or TorrentOrder.DownloadDirDesc => anchorValue is null or string,
            _ => throw new ArgumentOutOfRangeException(nameof(orderBy))
        };
    }
}
