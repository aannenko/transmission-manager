using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Api.Common.Attributes;

/// <summary>
/// Specifies that a value, a <see cref="Uri"/> or a string, must be an absolute HTTP or HTTPS address.
/// </summary>
/// <remarks>
/// <see cref="Uri"/> properties deserialize with <see cref="UriKind.RelativeOrAbsolute"/>, so
/// <c>[Required]</c> alone admits relative and non-web addresses that no HTTP client can fetch.
/// <see langword="null"/> is valid; use <c>[Required]</c> to enforce presence.
/// </remarks>
public sealed class HttpUriAttribute : ValidationAttribute
{
    /// <summary>Initializes a validator for absolute HTTP and HTTPS URIs.</summary>
    public HttpUriAttribute()
    {
        ErrorMessage = "Value must be an absolute http or https address.";
    }

    /// <summary>
    /// Determines whether the value is an absolute <c>http</c> or <c>https</c> address.
    /// </summary>
    /// <param name="value">The value to check: a <see cref="Uri"/>, or a string to parse.</param>
    /// <returns><see langword="true"/> for such an address, and for <see langword="null"/>.</returns>
    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            Uri uri => HttpUriUtils.IsHttpUri(uri),
            string text => HttpUriUtils.TryCreate(text, out _),
            _ => false,
        };
    }
}
