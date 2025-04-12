using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class FindTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/{id}", FindTorrentByIdAsync)
            .WithName(EndpointNames.FindTorrentById);

        return builder;
    }

    private static async Task<Results<Ok<TorrentDto>, ProblemHttpResult, ValidationProblem>> FindTorrentByIdAsync(
        [FromServices] TorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        var torrent = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return torrent is not null
            ? TypedResults.Ok(torrent.ToDto())
            : TypedResults.Problem(
                string.Format(null, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
