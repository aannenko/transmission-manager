using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal static class TorrentOrderExtensions
{
    public static bool IsOrderByString(this TorrentOrder orderBy)
    {
        return orderBy is
            TorrentOrder.Name or
            TorrentOrder.NameDesc or
            TorrentOrder.WebPage or
            TorrentOrder.WebPageDesc or
            TorrentOrder.DownloadDir or
            TorrentOrder.DownloadDirDesc;
    }
}
