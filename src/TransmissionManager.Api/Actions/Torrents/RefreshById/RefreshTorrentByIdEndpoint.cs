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
            [FromServices] IRefreshTorrentByIdHandler handler,
            long id,
            CancellationToken cancellationToken)
    {
        var (result, torrentDto, transmissionResult, message, currentVersion) = await handler
            .RefreshTorrentByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            RefreshTorrentByIdResult.Refreshed =>
                TypedResults.Ok(new RefreshTorrentByIdResponse(torrentDto!, transmissionResult!.Value, message)),
            RefreshTorrentByIdResult.NotFoundLocally or RefreshTorrentByIdResult.Removed =>
                TypedResults.Problem(message, statusCode: StatusCodes.Status404NotFound),
            RefreshTorrentByIdResult.NotFoundInTransmission =>
                TypedResults.Problem(message, statusCode: StatusCodes.Status422UnprocessableEntity),
            RefreshTorrentByIdResult.Conflict =>
                TypedResults.Problem(
                    message,
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: [new(ProblemDetailsExtensionKeys.CurrentVersion, currentVersion)]),
            RefreshTorrentByIdResult.DependencyFailed =>
                TypedResults.Problem(message, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
