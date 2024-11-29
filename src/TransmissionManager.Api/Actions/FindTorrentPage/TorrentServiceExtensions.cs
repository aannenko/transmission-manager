using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

internal static class TorrentServiceExtensions
{
    public static async Task<Torrent[]> FindPageAsync(
        this TorrentService service,
        FindTorrentPageParameters parameters,
        CancellationToken cancellationToken)
    {
        var (orderBy, take, afterId, after, _, _) = parameters;
        if (orderBy is TorrentOrder.Id)
        {
            var page = new TorrentPageDescriptor<bool>(orderBy, take, afterId);
            return await service.FindPageAsync(page, parameters.ToTorrentFilter(), cancellationToken)
                .ConfigureAwait(false);
        }

        if (orderBy.IsOrderByString())
        {
            var page = new TorrentPageDescriptor<string>(orderBy, take, afterId, after);
            return await service.FindPageAsync(page, parameters.ToTorrentFilter(), cancellationToken)
                .ConfigureAwait(false);
        }

        var error = $"Incompatible values of {nameof(parameters.OrderBy)} ({orderBy}) " +
            $"and {nameof(parameters.After)} ({after}) were provided.";

        throw new ArgumentException(error, nameof(parameters));
    }
}
