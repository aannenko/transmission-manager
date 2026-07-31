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
        var updateDto = request.ToTorrentUpdateDto();
        var (result, currentVersion, error) = await handler
            .TryUpdateTorrentByIdAsync(id, version, updateDto, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            UpdateTorrentByIdResult.Updated =>
                TypedResults.NoContent(),
            UpdateTorrentByIdResult.NotFound =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            UpdateTorrentByIdResult.VersionConflict =>
                TypedResults.Problem(
                    error,
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: [new(ProblemDetailsExtensionKeys.CurrentVersion, currentVersion)]),
            UpdateTorrentByIdResult.Exists =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status409Conflict),
            _ => throw new NotImplementedException(),
        };
    }
}
