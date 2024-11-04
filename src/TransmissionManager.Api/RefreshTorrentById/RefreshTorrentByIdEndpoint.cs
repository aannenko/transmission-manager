using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.RefreshTorrentById;

public static class RefreshTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapRefreshTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/{id}", RefreshTorrentByIdAsync)
            .WithName(EndpointNames.RefreshTorrentById);

        return builder;
    }

    private static async Task<Results<Ok<RefreshTorrentByIdResponse>, ProblemHttpResult, ValidationProblem>>
        RefreshTorrentByIdAsync(
            [FromServices] RefreshTorrentByIdHandler handler,
            long id,
            CancellationToken cancellationToken)
    {
        var (result, transmissionResult, error) = await handler
            .RefreshTorrentByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            RefreshTorrentByIdResult.TorrentRefreshed =>
                TypedResults.Ok(new RefreshTorrentByIdResponse(transmissionResult!.Value)),
            RefreshTorrentByIdResult.NotFoundLocally or RefreshTorrentByIdResult.Removed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            RefreshTorrentByIdResult.NotFoundInTransmission =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status422UnprocessableEntity),
            RefreshTorrentByIdResult.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
