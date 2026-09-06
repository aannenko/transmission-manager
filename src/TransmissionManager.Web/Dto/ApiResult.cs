using System.Diagnostics.CodeAnalysis;
using System.Net;
using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Dto;

#pragma warning disable CA1515 // Consider making public types internal - carried by public Blazor component parameters

/// <summary>
/// The outcome of an API call.
/// </summary>
/// <remarks>
/// <see cref="Failed"/> is first so <c>default</c> is a failure: a call that never reached the
/// server returns one, and reading that as success would report a change the server never made.
/// </remarks>
public enum ApiResultStatus
{
    Failed,
    Success,
    NotFound,
    Conflict,
}

/// <summary>
/// Represents API response without content.
/// </summary>
public readonly record struct ApiResult
{
    private ApiResult(ApiResultStatus status, HttpStatusCode? statusCode, ApiProblemDetails? problemDetails)
    {
        Status = status;
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }

    public ApiResultStatus Status { get; }

    /// <summary>
    /// The status the server answered with, or <see langword="null"/> if no response arrived.
    /// </summary>
    /// <remarks>
    /// Kept beside <see cref="Status"/> because a failure carrying no problem details - a proxy's
    /// HTML error page - has nothing else to report.
    /// </remarks>
    public HttpStatusCode? StatusCode { get; }

    public ApiProblemDetails? ProblemDetails { get; }

    /// <summary>
    /// The version the torrent holds now, when the API refused an outdated one.
    /// </summary>
    /// <remarks>
    /// Read out of the problem details rather than stored beside them, so the two cannot disagree.
    /// </remarks>
    public long? CurrentVersion => ProblemDetails?.CurrentVersion;

    public static ApiResult Success(HttpStatusCode statusCode) =>
        new(ApiResultStatus.Success, statusCode, null);

    public static ApiResult Failure(HttpStatusCode? statusCode, ApiProblemDetails? problemDetails) =>
        new(statusCode.ToFailureStatus(), statusCode, problemDetails);
}

/// <summary>
/// Represents API response with content.
/// </summary>
/// <typeparam name="T">
/// The response content. Constrained to a reference type so <see cref="Value"/> is genuinely optional.
/// </typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "The factories are the only way to build one, and they belong with the type whose invariants they keep.")]
public readonly record struct ApiResult<T>
    where T : class
{
    private ApiResult(ApiResultStatus status, HttpStatusCode? statusCode, T? value, ApiProblemDetails? problemDetails)
    {
        Status = status;
        StatusCode = statusCode;
        Value = value;
        ProblemDetails = problemDetails;
    }

    public ApiResultStatus Status { get; }

    /// <inheritdoc cref="ApiResult.StatusCode"/>
    public HttpStatusCode? StatusCode { get; }

    public T? Value { get; }

    public ApiProblemDetails? ProblemDetails { get; }

    /// <inheritdoc cref="ApiResult.CurrentVersion"/>
    public long? CurrentVersion => ProblemDetails?.CurrentVersion;

    public static ApiResult<T> Success(HttpStatusCode statusCode, T value) =>
        new(ApiResultStatus.Success, statusCode, value, null);

    /// <inheritdoc cref="ApiResult.Failure"/>
    public static ApiResult<T> Failure(HttpStatusCode? statusCode, ApiProblemDetails? problemDetails) =>
        new(statusCode.ToFailureStatus(), statusCode, null, problemDetails);
}
#pragma warning restore CA1515
