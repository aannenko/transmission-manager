using System.Net;
using TransmissionManager.Web.Dto;

namespace TransmissionManager.Web.Extensions;

internal static class HttpStatusCodeExtensions
{
    /// <summary>
    /// Maps a status code to the outcome a failed call reports.
    /// </summary>
    /// <param name="statusCode">The status the server answered with, if any.</param>
    /// <returns>The outcome, which is <see cref="ApiResultStatus.Failed"/> unless callers act on it.</returns>
    /// <remarks>
    /// A success code lands on <see cref="ApiResultStatus.Failed"/> too: a response whose body could
    /// not be read reaches here carrying one.
    /// </remarks>
    public static ApiResultStatus ToFailureStatus(this HttpStatusCode? statusCode) =>
        statusCode switch
        {
            HttpStatusCode.NotFound => ApiResultStatus.NotFound,
            HttpStatusCode.Conflict => ApiResultStatus.Conflict,
            _ => ApiResultStatus.Failed,
        };

    public static bool IsSuccessCode(this HttpStatusCode statusCode) =>
        (int)statusCode is >= 200 and <= 299;
}
