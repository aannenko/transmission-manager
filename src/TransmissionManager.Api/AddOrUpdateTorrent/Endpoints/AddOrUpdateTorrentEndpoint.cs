using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.AddOrUpdateTorrent.Handlers;
using TransmissionManager.Api.AddOrUpdateTorrent.Request;
using TransmissionManager.Api.Common.Constants;
using AddOrUpdateResult = TransmissionManager.Api.AddOrUpdateTorrent.Handlers.AddOrUpdateTorrentResult.ResultType;

namespace TransmissionManager.Api.AddOrUpdateTorrent.Endpoints;

public static class AddOrUpdateTorrentEndpoint
{
    public static IEndpointRouteBuilder MapAddOrUpdateTorrentEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointAddresses.TorrentsApi}/", AddOrUpdateOneAsync);
        return builder;
    }

    private static async Task<Results<Created, NoContent, BadRequest<string>, ValidationProblem>> AddOrUpdateOneAsync(
        [FromServices] AddOrUpdateTorrentHandler handler,
        TorrentAddOrUpdateRequest dto,
        CancellationToken cancellationToken)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var (resultType, id, errorMessage) =
            await handler.AddOrUpdateTorrentAsync(dto, cancellationToken).ConfigureAwait(false);

        return resultType switch
        {
            AddOrUpdateResult.Add => TypedResults.Created($"{EndpointAddresses.TorrentsApi}/{id}"),
            AddOrUpdateResult.Update => TypedResults.NoContent(),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }
}
