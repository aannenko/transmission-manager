using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.FindTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.FindTorrentPageOrder;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class FindTorrentPageParametersExtensions
{
    public static TorrentPageDescriptor<string> ToTorrentPageDescriptor(in this FindTorrentPageParameters parameters)
    {
        return new TorrentPageDescriptor<string>(
            OrderBy: (TorrentOrder)parameters.OrderBy,
            AnchorId: parameters.AnchorId,
            AnchorValue: parameters.AnchorValue,
            IsForwardPagination: parameters.Direction is Direction.Forward,
            Take: parameters.Take);
    }

    public static TorrentFilter ToTorrentFilter(in this FindTorrentPageParameters parameters)
    {
        return new(
            PropertyStartsWith: parameters.PropertyStartsWith,
            CronExists: parameters.CronExists);
    }

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
                    Order.Name or Order.NameDesc => currentPage[0].Name,
                    Order.WebPage or Order.WebPageDesc => currentPage[0].WebPageUri.OriginalString,
                    Order.DownloadDir or Order.DownloadDirDesc => currentPage[0].DownloadDir,
                    _ => null,
                },
                Direction = Direction.Backward
            };
    }
}
