using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.FindTorrentPage.Extensions;
using TransmissionManager.Api.FindTorrentPage.Request;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.FindTorrentPage.Endpoints;

public static class FindTorrentPageEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentPageEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointAddresses.TorrentsApi}/", FindTorrentPageAsync);
        return builder;
    }

    private static async Task<Torrent[]> FindTorrentPageAsync(
        [FromServices] TorrentQueryService service,
        [AsParameters] FindTorrentPageParameters parameters,
        CancellationToken cancellationToken)
    {
        return await service
            .FindPageAsync(parameters.ToPageDescriptor(), parameters.ToTorrentFilter(), cancellationToken)
            .ConfigureAwait(false);
    }
}
