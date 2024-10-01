using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.UpdateTorrentById;

public static class UpdateTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPatch($"{EndpointAddresses.TorrentsApi}/{{id}}", UpdateTorrentByIdAsync)
            .WithName(EndpointNames.UpdateTorrentById);

        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] UpdateTorrentByIdHandler service,
        long id,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        bool areThereErrors = !MiniValidator.TryValidate(request, out var errors);
        if (id < 1)
        {
            areThereErrors = true;
            errors = new Dictionary<string, string[]>(errors)
            {
                [nameof(id)] = [EndpointMessages.ValueMustBeGreaterThanZero]
            };
        }

        if (areThereErrors)
            return TypedResults.ValidationProblem(errors);

        var updateDto = request.ToTorrentUpdateDto();
        return await service.TryUpdateTorrentByIdAsync(id, updateDto, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                string.Format(EndpointMessages.IdNotFound, id),
                statusCode: StatusCodes.Status404NotFound);
    }
}
