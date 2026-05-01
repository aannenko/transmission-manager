using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Constants;
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
        [FromQuery, Required, Range(1L, long.MaxValue)] long version,
        [EnumDataType(typeof(DeleteTorrentByIdType))] DeleteTorrentByIdType deleteType = DeleteTorrentByIdType.Local,
        CancellationToken cancellationToken = default)
    {
        var (result, currentVersion, error) = await handler
            .TryDeleteTorrentByIdAsync(id, version, deleteType, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            DeleteTorrentByIdResult.Deleted =>
                TypedResults.NoContent(),
            DeleteTorrentByIdResult.NotFound =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status404NotFound),
            DeleteTorrentByIdResult.Conflict =>
                TypedResults.Problem(
                    error,
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: [new(ProblemDetailsExtensionKeys.CurrentVersion, currentVersion)]),
            DeleteTorrentByIdResult.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
