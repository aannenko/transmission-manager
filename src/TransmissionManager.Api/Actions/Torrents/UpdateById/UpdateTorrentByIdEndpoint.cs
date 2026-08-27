using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal static class UpdateTorrentByIdEndpoint
{
    internal const string IdParamName = "id";
    internal const string VersionParamName = "version";

    public static IEndpointRouteBuilder MapUpdateTorrentByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapPatch("/{id}", UpdateTorrentByIdAsync).WithName(EndpointNames.UpdateTorrentById);
        return builder;
    }

    private static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> UpdateTorrentByIdAsync(
        [FromServices] UpdateTorrentByIdHandler handler,
        long id,
        [FromQuery, Required, Range(1L, long.MaxValue)] long version,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken)
    {
        var (result, currentVersion, errors) = await handler
            .TryUpdateTorrentByIdAsync(id, version, request, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            UpdateTorrentByIdResult.Updated =>
                TypedResults.NoContent(),
            UpdateTorrentByIdResult.InvalidRequest =>
                TypedResults.ValidationProblem(errors),
            UpdateTorrentByIdResult.NotFound =>
                Problem(errors, StatusCodes.Status404NotFound),
            UpdateTorrentByIdResult.Conflict =>
                Conflict(errors, currentVersion),
            _ => throw new NotImplementedException(),
        };
    }

    /// <remarks>
    /// The current version is present only when resubmitting against it can resolve the conflict,
    /// which is what lets a caller tell a lost race from a collision it has to fix.
    /// </remarks>
    private static ProblemHttpResult Conflict(KeyValuePair<string, string[]>[] errors, long? currentVersion) =>
        currentVersion is null
            ? Problem(errors, StatusCodes.Status409Conflict)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                extensions:
                [
                    new(ProblemDetailsExtensionKeys.Errors, ToErrorsDictionary(errors)),
                    new(ProblemDetailsExtensionKeys.CurrentVersion, currentVersion),
                ]);

    private static ProblemHttpResult Problem(KeyValuePair<string, string[]>[] errors, int statusCode) =>
        TypedResults.Problem(
            statusCode: statusCode,
            extensions: [new(ProblemDetailsExtensionKeys.Errors, ToErrorsDictionary(errors))]);

    /// <remarks>
    /// The pairs have to become a dictionary to reach the client as the object a validation failure
    /// arrives in - serialized as they are, they would be an array of <c>Key</c>/<c>Value</c>
    /// objects instead. <c>TypedResults.ValidationProblem</c> does this conversion itself; a
    /// response carrying any other status code has to do it here.
    /// </remarks>
    private static Dictionary<string, string[]> ToErrorsDictionary(KeyValuePair<string, string[]>[] errors) =>
        new(errors, StringComparer.Ordinal);
}
