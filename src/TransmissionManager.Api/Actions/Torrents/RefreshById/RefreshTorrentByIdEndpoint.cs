using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
            [FromServices] IRefreshTorrentByIdHandler handler,
            long id,
            CancellationToken cancellationToken)
    {
        var (result, torrentDto, transmissionResult, warning, errors, currentVersion) = await handler
            .RefreshTorrentByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            RefreshTorrentByIdResult.Refreshed =>
                TypedResults.Ok(new RefreshTorrentByIdResponse(torrentDto!, transmissionResult!.Value, warning)),
            RefreshTorrentByIdResult.NotFoundLocally or RefreshTorrentByIdResult.Removed =>
                EndpointProblems.Problem(errors, StatusCodes.Status404NotFound),
            RefreshTorrentByIdResult.NotFoundInTransmission or RefreshTorrentByIdResult.InvalidConfiguration =>
                EndpointProblems.Problem(errors, StatusCodes.Status422UnprocessableEntity),
            RefreshTorrentByIdResult.VersionConflict =>
                EndpointProblems.Conflict(errors, currentVersion),
            RefreshTorrentByIdResult.Exists =>
                EndpointProblems.Problem(errors, StatusCodes.Status409Conflict),
            RefreshTorrentByIdResult.DependencyFailed =>
                EndpointProblems.Problem(errors, StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
