using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.RefreshTorrentById.Handlers;
using RefreshResult = TransmissionManager.Api.RefreshTorrentById.Handlers.RefreshTorrentByIdResult.ResultType;

namespace TransmissionManager.Api.RefreshTorrentById.Endpoints;

public static class RefreshTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapRefreshTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointAddresses.TorrentsApi}/{{id}}", RefreshTorrentByIdAsync);
        return builder;
    }

    private static async Task<Results<NoContent, NotFound<string>, BadRequest<string>>> RefreshTorrentByIdAsync(
        [FromServices] RefreshTorrentByIdHandler handler,
        long id,
        CancellationToken cancellationToken)
    {
        var (resultType, errorMessage) =
            await handler.RefreshTorrentByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return resultType switch
        {
            RefreshResult.Success => TypedResults.NoContent(),
            RefreshResult.NotFound => TypedResults.NotFound(string.Format(EndpointMessages.IdNotFound, id)),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }
}
