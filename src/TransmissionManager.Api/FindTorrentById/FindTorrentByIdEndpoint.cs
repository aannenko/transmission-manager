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
        builder.MapGet($"{EndpointAddresses.TorrentsApi}/{{id}}", FindTorrentByIdAsync)
            .WithName(EndpointNames.FindTorrentById);

        return builder;
    }

    private static async Task<Results<Ok<Torrent>, ProblemHttpResult, ValidationProblem>> FindTorrentByIdAsync(
        [FromServices] TorrentQueryService service,
        long id,
        CancellationToken cancellationToken)
    {
        if (id < 1)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(id)] = [EndpointMessages.ValueMustBeGreaterThanZero]
            });
        }

        var torrent = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return torrent is not null
            ? TypedResults.Ok(torrent)
            : TypedResults.Problem(
                string.Format(EndpointMessages.IdNotFound, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
