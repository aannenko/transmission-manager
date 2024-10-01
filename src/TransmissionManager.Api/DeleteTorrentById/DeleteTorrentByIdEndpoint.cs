using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.DeleteTorrentById;

public static class DeleteTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapDelete($"{EndpointAddresses.TorrentsApi}/{{id}}", DeleteTorrentByIdAsync)
            .WithName(EndpointNames.DeleteTorrentById);

        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> DeleteTorrentByIdAsync(
        [FromServices] DeleteTorrentByIdHandler service,
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

        return await service.TryDeleteTorrentByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                string.Format(EndpointMessages.IdNotFound, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
