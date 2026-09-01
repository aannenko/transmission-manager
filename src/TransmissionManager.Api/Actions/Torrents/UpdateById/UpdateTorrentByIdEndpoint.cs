using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
        var (result, currentVersion, errors) = await handler
            .TryUpdateTorrentByIdAsync(id, version, request, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            UpdateTorrentByIdResult.Updated =>
                TypedResults.NoContent(),
            UpdateTorrentByIdResult.InvalidRequest =>
                TypedResults.ValidationProblem(errors),
            UpdateTorrentByIdResult.NotFound =>
                EndpointProblems.Problem(errors, StatusCodes.Status404NotFound),
            UpdateTorrentByIdResult.Conflict =>
                EndpointProblems.Conflict(errors, currentVersion),
            _ => throw new NotImplementedException(),
        };
    }
}
