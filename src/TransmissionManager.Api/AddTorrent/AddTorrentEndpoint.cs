﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.AddTorrent;

public static class AddTorrentEndpoint
{
    public static IEndpointRouteBuilder MapAddTorrentEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointAddresses.TorrentsApi}/", AddTorrentAsync)
            .WithName(EndpointNames.AddTorrent);

        return builder;
    }

    private static async Task<Results<Created<AddTorrentResponse>, ProblemHttpResult, ValidationProblem>>
        AddTorrentAsync(
            [FromServices] LinkGenerator linker,
            [FromServices] AddTorrentHandler handler,
            AddTorrentRequest dto,
            CancellationToken cancellationToken)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var (result, id, transmissionResult, error) = await handler
            .AddTorrentAsync(dto, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            AddTorrentHandler.Result.TorrentAdded =>
                TypedResults.Created(GetTorrentUri(linker, id), new AddTorrentResponse(transmissionResult!.Value)),
            AddTorrentHandler.Result.TorrentExists =>
                TypedResults.Problem(
                    error,
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { [nameof(transmissionResult)] = transmissionResult }),
            AddTorrentHandler.Result.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException()
        };
    }

    private static string? GetTorrentUri(LinkGenerator linker, long? id) =>
        linker.GetPathByName(EndpointNames.FindTorrentById, new() { [nameof(id)] = id });
}
