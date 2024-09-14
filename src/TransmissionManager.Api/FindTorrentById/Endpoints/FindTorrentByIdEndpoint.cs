using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.FindTorrentById.Endpoints;

public static class FindTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointAddresses.TorrentsApi}/{{id}}", FindTorrentByIdAsync);
        return builder;
    }

    private static async Task<Results<Ok<Torrent>, NotFound<string>>> FindTorrentByIdAsync(
        [FromServices] TorrentQueryService service,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound(string.Format(EndpointMessages.IdNotFound, id));
    }
}
