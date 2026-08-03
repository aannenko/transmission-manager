using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

/// <summary>
/// Specifies that a <see cref="Uri"/> value must be an absolute HTTP or HTTPS address.
/// </summary>
/// <remarks>
/// <see cref="Uri"/> properties deserialize with <see cref="UriKind.RelativeOrAbsolute"/>, so
/// <c>[Required]</c> alone admits relative and non-web addresses that no HTTP client can fetch.
/// <see langword="null"/> is valid; use <c>[Required]</c> to enforce presence.
/// </remarks>
public sealed class HttpUriAttribute : ValidationAttribute
{
    public HttpUriAttribute()
    {
        ErrorMessage = "Value must be an absolute http or https address.";
    }

    /// <summary>
    /// Determines whether the specified value is an absolute HTTP or HTTPS address.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>Whether the value is valid.</returns>
    public override bool IsValid(object? value) =>
        value is null ||
        (value is Uri uri &&
            uri.IsAbsoluteUri &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}
