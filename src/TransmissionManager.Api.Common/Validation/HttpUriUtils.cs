using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Api.Common.Validation;

/// <summary>
/// Recognizes absolute HTTP and HTTPS addresses.
/// </summary>
public static class HttpUriUtils
{
    /// <summary>
    /// Parses a string as an absolute <c>http</c> or <c>https</c> address, ignoring whitespace around it.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="uri">The address, when <paramref name="value"/> is one.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is such an address.</returns>
    public static bool TryCreate([NotNullWhen(true)] string? value, [NotNullWhen(true)] out Uri? uri)
    {
        // Uri.TryCreate ignores spaces around an address, but not non-breaking ones.
        return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out uri) && IsHttpUri(uri);
    }

    internal static bool IsHttpUri(Uri uri) =>
        uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
