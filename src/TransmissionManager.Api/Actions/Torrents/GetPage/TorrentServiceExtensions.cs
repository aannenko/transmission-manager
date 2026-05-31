using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.GetPage;

internal static class TorrentServiceExtensions
{
    public static Task<TorrentPage> GetPageAsync(
        this TorrentService service,
        in GetTorrentPageParameters parameters,
        in GetTorrentPageParsedParameters parsedParameters,
        CancellationToken cancellationToken = default)
    {
        var filter = parameters.ToTorrentFilter();
        if (parsedParameters.DateTimeAnchor is not null)
        {
            var pageDescriptor = GetPageDescriptor(parameters, parsedParameters.DateTimeAnchor);
            return service.GetPageAsync(pageDescriptor, filter, cancellationToken);
        }
        else
        {
            var pageDescriptor = GetPageDescriptor(parameters, parameters.AnchorValue);
            return service.GetPageAsync(pageDescriptor, filter, cancellationToken);
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
            Direction: (PaginationDirection)parameters.Direction,
            Take: parameters.Take);
    }
}
