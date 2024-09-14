using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Services;

namespace TransmissionManager.Api.DeleteTorrentById.Endpoints;

public static class DeleteTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapDelete($"{EndpointAddresses.TorrentsApi}/{{id}}", DeleteTorrentByIdAsync);
        return builder;
    }

    private static async Task<Results<NoContent, NotFound<string>>> DeleteTorrentByIdAsync(
        [FromServices] SchedulableTorrentCommandService service,
        long id,
        CancellationToken cancellationToken)
    {
        return await service.TryDeleteOneByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound(string.Format(EndpointMessages.IdNotFound, id));
    }
}
