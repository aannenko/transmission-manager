using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal static class UpdateTorrentByIdEndpoint
{
    private const string _concurrencyConflictMessage =
        "The torrent was modified by another request. Re-read it and retry the update.";

    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapPatch("/{id}", UpdateTorrentByIdAsync).WithName(EndpointNames.UpdateTorrentById);
        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] UpdateTorrentByIdHandler handler,
        long id,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        var updateDto = request.ToTorrentUpdateDto();
        var result = await handler
            .TryUpdateTorrentByIdAsync(id, updateDto, request.Version, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            TorrentUpdateResult.Updated => TypedResults.NoContent(),
            TorrentUpdateResult.NotFound => TypedResults.Problem(
                string.Format(CultureInfo.InvariantCulture, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound),
            TorrentUpdateResult.ConcurrencyConflict => TypedResults.Problem(
                _concurrencyConflictMessage,
                statusCode: StatusCodes.Status409Conflict),
            _ => throw new NotImplementedException(),
        };
    }
}
