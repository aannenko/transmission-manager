using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Actions.UpdateTorrentById;

public static class UpdateTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPatch("/{id}", UpdateTorrentByIdAsync)
            .WithParameterValidation()
            .WithName(EndpointNames.UpdateTorrentById);

        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] UpdateTorrentByIdHandler service,
        long id,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        var updateDto = request.ToTorrentUpdateDto();
        return await service.TryUpdateTorrentByIdAsync(id, updateDto, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                string.Format(null, EndpointMessages.IdNotFoundFormat, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
