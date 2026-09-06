using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Web.Dto;

#pragma warning disable CA1515 // Consider making public types internal - carried by the public ApiResult

/// <summary>
/// The valuable parts of an API Problem Details response.
/// </summary>
/// <remarks>
/// Unknown members are ignored, so a response carrying none of these deserializes into an instance
/// with every property <see langword="null"/>; check the one you need rather than the instance.
/// </remarks>
/// <param name="Errors">What is wrong, keyed by what is at fault.</param>
/// <param name="CurrentVersion">The version the torrent holds now, when an outdated one was sent.</param>
/// <param name="TransmissionResult">
/// What Transmission did with the magnet before the request failed.
/// </param>
public sealed record ApiProblemDetails(
    Dictionary<string, string[]?>? Errors,
    long? CurrentVersion,
    TransmissionAddResult? TransmissionResult);
#pragma warning restore CA1515
