using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.DeleteTorrentById;

public static class DeleteTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapDelete("/{id}", DeleteTorrentByIdAsync)
            .WithName(EndpointNames.DeleteTorrentById);

        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> DeleteTorrentByIdAsync(
        [FromServices] DeleteTorrentByIdHandler service,
        long id,
        CancellationToken cancellationToken)
    {
        return await service.TryDeleteTorrentByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                string.Format(null, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
