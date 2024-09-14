using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.UpdateTorrentById.Extensions;
using TransmissionManager.Api.UpdateTorrentById.Request;

namespace TransmissionManager.Api.UpdateTorrentById.Endpoints;

public static class UpdateTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPatch($"{EndpointAddresses.TorrentsApi}/{{id}}", UpdateTorrentByIdAsync);
        return builder;
    }

    private static async Task<Results<NoContent, NotFound<string>, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] SchedulableTorrentCommandService service,
        long id,
        TorrentPatchRequest dto,
        CancellationToken cancellationToken)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var updateDto = dto.ToTorrentUpdateDto();
        return await service.TryUpdateOneByIdAsync(id, updateDto, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound(string.Format(EndpointMessages.IdNotFound, id));
    }
}
