﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Constants;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class UpdateTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPatch("/{id}", UpdateTorrentByIdAsync)
            .WithParameterValidation()
            .WithName(EndpointNames.UpdateTorrentById);

        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] UpdateTorrentByIdHandler handler,
        long id,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        var updateDto = request.ToTorrentUpdateDto();
        return await handler.TryUpdateTorrentByIdAsync(id, updateDto, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                string.Format(CultureInfo.InvariantCulture, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
