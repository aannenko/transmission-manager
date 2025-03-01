﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.FindById;

internal static class FindTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/{id}", FindTorrentByIdAsync)
            .WithName(EndpointNames.FindTorrentById);

        return builder;
    }

    private static async Task<Results<Ok<Torrent>, ProblemHttpResult, ValidationProblem>> FindTorrentByIdAsync(
        [FromServices] TorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        var torrent = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return torrent is not null
            ? TypedResults.Ok(torrent)
            : TypedResults.Problem(
                string.Format(null, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
