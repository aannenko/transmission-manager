using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.GetById;

internal static class GetTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapGet("/{id}", GetTorrentByIdAsync).WithName(EndpointNames.GetTorrentById);
        return builder;
    }

    private static async Task<Results<Ok<TorrentDto>, ProblemHttpResult, ValidationProblem>> GetTorrentByIdAsync(
        [FromServices] TorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        var torrent = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return torrent is not null
            ? TypedResults.Ok(torrent.ToDto())
            : TypedResults.Problem(
                string.Format(CultureInfo.InvariantCulture, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
