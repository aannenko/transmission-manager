using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Actions;

/// <remarks>
/// Input validation failures arrive with their messages in the <c>errors</c> object through
/// <c>TypedResults.ValidationProblem</c>; these build the same shape for the hand-written ones.
/// </remarks>
internal static class EndpointProblems
{
    public static ProblemHttpResult Problem(KeyValuePair<string, string[]>[] errors, int statusCode)
    {
        return TypedResults.Problem(statusCode: statusCode, extensions: [ToErrorsExtension(errors)]);
    }

    public static ProblemHttpResult Problem(
        KeyValuePair<string, string[]>[] errors,
        int statusCode,
        KeyValuePair<string, object?> extension)
    {
        return TypedResults.Problem(statusCode: statusCode, extensions: [ToErrorsExtension(errors), extension]);
    }

    /// <remarks>
    /// The current version is present only when resubmitting against it can resolve the conflict,
    /// which is what lets a caller tell a lost race from a collision it has to fix.
    /// </remarks>
    public static ProblemHttpResult Conflict(KeyValuePair<string, string[]>[] errors, long? currentVersion)
    {
        return currentVersion is null
            ? Problem(errors, StatusCodes.Status409Conflict)
            : Problem(
                errors,
                StatusCodes.Status409Conflict,
                new(ProblemDetailsKeys.CurrentVersion, currentVersion));
    }

    /// <remarks>
    /// The pairs have to become a dictionary to reach the client as the object a validation failure
    /// arrives in - serialized as they are, they would be an array of <c>Key</c>/<c>Value</c>
    /// objects instead. <c>TypedResults.ValidationProblem</c> does this conversion itself; a
    /// response carrying any other status code has to do it here.
    /// </remarks>
    private static KeyValuePair<string, object?> ToErrorsExtension(KeyValuePair<string, string[]>[] errors) =>
        new(ProblemDetailsKeys.Errors, new Dictionary<string, string[]>(errors, StringComparer.Ordinal));
}
