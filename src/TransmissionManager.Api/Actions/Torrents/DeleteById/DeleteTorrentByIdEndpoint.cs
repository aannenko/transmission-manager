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
        [FromQuery, Required, Range(1L, long.MaxValue)] long version,
        [EnumDataType(typeof(DeleteTorrentByIdType))] DeleteTorrentByIdType deleteType = DeleteTorrentByIdType.Local,
        CancellationToken cancellationToken = default)
    {
        var (result, currentVersion, errors) = await handler
            .TryDeleteTorrentByIdAsync(id, version, deleteType, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            DeleteTorrentByIdResult.Deleted =>
                TypedResults.NoContent(),
            DeleteTorrentByIdResult.NotFound =>
                EndpointProblems.Problem(errors, StatusCodes.Status404NotFound),
            DeleteTorrentByIdResult.VersionConflict =>
                EndpointProblems.Conflict(errors, currentVersion),
            DeleteTorrentByIdResult.DependencyFailed =>
                EndpointProblems.Problem(errors, StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException(),
        };
    }
}
