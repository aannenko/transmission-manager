using Direction = TransmissionManager.Api.Common.Dto.Torrents.FindTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.FindTorrentPageOrder;

namespace TransmissionManager.Api.Common.Dto.Torrents;

internal static class FindTorrentPageParametersExtensions
{
    public static FindTorrentPageParameters? ToNextPageParameters(
        in this FindTorrentPageParameters parameters,
        TorrentDto[] currentPage)
    {
        return currentPage.Length is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[^1].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    Order.Id or Order.IdDesc => null,
                    Order.RefreshDate or Order.RefreshDateDesc => currentPage[^1].RefreshDate.ToString("o"),
                    Order.Name or Order.NameDesc => currentPage[^1].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[^1].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[^1].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Forward
            };
    }

    public static FindTorrentPageParameters? ToPreviousPageParameters(
        in this FindTorrentPageParameters parameters,
        TorrentDto[] currentPage)
    {
        return currentPage.Length is 0
            ? null
            : parameters with
            {
                AnchorId = currentPage[0].Id,
                AnchorValue = parameters.OrderBy switch
                {
                    Order.Id or Order.IdDesc => null,
                    Order.RefreshDate or Order.RefreshDateDesc => currentPage[0].RefreshDate.ToString("o"),
                    Order.Name or Order.NameDesc => currentPage[0].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[0].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[0].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Backward
            };
    }
}
