using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.RefreshTorrentById;

public static class RefreshTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapRefreshTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointAddresses.TorrentsApi}/{{id}}", RefreshTorrentByIdAsync)
            .WithName(EndpointNames.RefreshTorrentById);

        return builder;
    }

    private static async Task<Results<Ok<RefreshTorrentByIdResponse>, ProblemHttpResult, ValidationProblem>>
        RefreshTorrentByIdAsync(
            [FromServices] RefreshTorrentByIdHandler handler,
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

        var (result, transmissionResult, error) = await handler
            .RefreshTorrentByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            RefreshTorrentByIdHandler.Result.TorrentRefreshed =>
                TypedResults.Ok(new RefreshTorrentByIdResponse(transmissionResult!.Value)),
            RefreshTorrentByIdHandler.Result.NotFoundLocally or RefreshTorrentByIdHandler.Result.Removed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            RefreshTorrentByIdHandler.Result.NotFoundInTransmission =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status422UnprocessableEntity),
            RefreshTorrentByIdHandler.Result.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
