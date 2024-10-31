using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.FindTorrentById;

public static class FindTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/{id}", FindTorrentByIdAsync)
            .WithName(EndpointNames.FindTorrentById);

        return builder;
    }

    private static async Task<Results<Ok<Torrent>, ProblemHttpResult, ValidationProblem>> FindTorrentByIdAsync(
        [FromServices] TorrentQueryService service,
        long id,
        CancellationToken cancellationToken)
    {
        var torrent = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return torrent is not null
            ? TypedResults.Ok(torrent)
            : TypedResults.Problem(
                string.Format(EndpointMessages.IdNotFound, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
