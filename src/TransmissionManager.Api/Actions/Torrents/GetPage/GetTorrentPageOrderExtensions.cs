using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents.GetPage;

internal static class GetTorrentPageOrderExtensions
{
    public static bool IsDescending(this GetTorrentPageOrder orderBy)
    {
        return orderBy is
            GetTorrentPageOrder.IdDesc or
            GetTorrentPageOrder.RefreshDateDesc or
            GetTorrentPageOrder.NameDesc or
            GetTorrentPageOrder.UriDesc or
            GetTorrentPageOrder.DownloadDirDesc;
    }
}
