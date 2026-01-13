using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents.DeleteById;

internal static class DeleteTorrentByIdEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapDelete("/{id}", DeleteTorrentByIdAsync).WithName(EndpointNames.DeleteTorrentById);
        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> DeleteTorrentByIdAsync(
        [FromServices] DeleteTorrentByIdHandler handler,
        long id,
        [EnumDataType(typeof(DeleteTorrentByIdType))] DeleteTorrentByIdType deleteType = DeleteTorrentByIdType.Local,
        CancellationToken cancellationToken = default)
    {
        var (result, error) = await handler
            .TryDeleteTorrentByIdAsync(id, deleteType, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            DeleteTorrentByIdResult.Removed =>
                TypedResults.NoContent(),
            DeleteTorrentByIdResult.NotFoundLocally =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            DeleteTorrentByIdResult.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
