using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Serialization;

namespace TransmissionManager.Web.Extensions;

internal static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Reads a response that carries content into a result.
    /// </summary>
    /// <param name="response">The response to read.</param>
    /// <param name="jsonTypeInfo">Describes the content this response should carry.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A success holding the content, or a failure holding whatever the response said is wrong.
    /// </returns>
    /// <remarks>
    /// A body that cannot be read is a failure even under a success status: the caller asked for a
    /// value, and there is none. This is the likeliest way to meet an address that answers but is
    /// not this API - a router page, or another application on the port.
    /// </remarks>
    public static async Task<ApiResult<T>> ToApiResultAsync<T>(
        this HttpResponseMessage response,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
        where T : class
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var value = await response.Content
                    .ReadFromJsonAsync(jsonTypeInfo, cancellationToken)
                    .ConfigureAwait(false);

                return value is null
                    ? ApiResult<T>.Failure(response.StatusCode, null)
                    : ApiResult<T>.Success(response.StatusCode, value);
            }
            catch (Exception e) when (IsUnreadableBody(e))
            {
                return ApiResult<T>.Failure(response.StatusCode, null);
            }
        }

        var problemDetails = await response.ReadApiProblemDetailsAsync(cancellationToken).ConfigureAwait(false);
        return ApiResult<T>.Failure(response.StatusCode, problemDetails);
    }

    /// <summary>
    /// Reads a response that carries no content of its own into a result.
    /// </summary>
    public static async Task<ApiResult> ToApiResultAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return ApiResult.Success(response.StatusCode);

        var problemDetails = await response.ReadApiProblemDetailsAsync(cancellationToken).ConfigureAwait(false);
        return ApiResult.Failure(response.StatusCode, problemDetails);
    }

    private static async Task<ApiProblemDetails?> ReadApiProblemDetailsAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content
                .ReadFromJsonAsync(WebJsonSerializerContext.Default.ApiProblemDetails, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e) when (IsUnreadableBody(e))
        {
            return null;
        }
    }

    /// <remarks>
    /// Exceptions raised during a body read:
    ///   invalid JSON - <see cref="JsonException"/>;
    ///   an unresolvable charset <c>windows-1252</c> or a <c>utf8</c> typo - <see cref="InvalidOperationException"/>;
    ///   UTF-7 - <see cref="NotSupportedException"/>.
    /// </remarks>
    private static bool IsUnreadableBody(Exception exception) =>
        exception is JsonException or InvalidOperationException or NotSupportedException;
}
