using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal static class UpdateTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapPatch("/{id}", UpdateTorrentByIdAsync).WithName(EndpointNames.UpdateTorrentById);
        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] UpdateTorrentByIdHandler handler,
        long id,
        [FromQuery, Required, Range(1L, long.MaxValue)] long version,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        var (result, currentVersion, error) = await handler
            .TryUpdateTorrentByIdAsync(id, version, request, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            UpdateTorrentByIdResult.Updated =>
                TypedResults.NoContent(),
            UpdateTorrentByIdResult.NotFound =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            UpdateTorrentByIdResult.Conflict =>
                Conflict(error, currentVersion),
            _ => throw new NotImplementedException(),
        };
    }

    /// <remarks>
    /// The current version is present only when resubmitting against it can resolve the conflict,
    /// which is what lets a caller tell a lost race from a collision it has to fix.
    /// </remarks>
    private static ProblemHttpResult Conflict(string? error, long? currentVersion) =>
        currentVersion is null
            ? TypedResults.Problem(error, statusCode: StatusCodes.Status409Conflict)
            : TypedResults.Problem(
                error,
                statusCode: StatusCodes.Status409Conflict,
                extensions: [new(ProblemDetailsExtensionKeys.CurrentVersion, currentVersion)]);
}
