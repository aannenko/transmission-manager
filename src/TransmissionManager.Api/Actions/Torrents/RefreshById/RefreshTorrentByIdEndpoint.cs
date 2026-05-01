using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents.RefreshById;

internal static class RefreshTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapRefreshTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapPost("/{id}", RefreshTorrentByIdAsync).WithName(EndpointNames.RefreshTorrentById);
        return builder;
    }

    private static async Task<Results<Ok<RefreshTorrentByIdResponse>, ProblemHttpResult, ValidationProblem>>
        RefreshTorrentByIdAsync(
            [FromServices] RefreshTorrentByIdHandler handler,
            long id,
            CancellationToken cancellationToken)
    {
        var (result, torrentDto, transmissionResult, error, currentVersion) = await handler
            .RefreshTorrentByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            RefreshTorrentByIdResult.Refreshed =>
                TypedResults.Ok(new RefreshTorrentByIdResponse(torrentDto!, transmissionResult!.Value)),
            RefreshTorrentByIdResult.NotFoundLocally or RefreshTorrentByIdResult.Removed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            RefreshTorrentByIdResult.NotFoundInTransmission =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status422UnprocessableEntity),
            RefreshTorrentByIdResult.Conflict =>
                TypedResults.Problem(
                    error,
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: [new(ProblemDetailsExtensionKeys.CurrentVersion, currentVersion)]),
            RefreshTorrentByIdResult.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
