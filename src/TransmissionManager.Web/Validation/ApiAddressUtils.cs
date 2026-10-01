using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Web.Validation;

internal static class ApiAddressUtils
{
    public static bool TryCreate(string value, [NotNullWhen(true)] out Uri? address)
    {
        var trimmed = value.Trim();

        // Only "://" marks a scheme: Uri reads "nas:9092" as scheme "nas", and prefixing every
        // scheme other than http(s) would turn "ftp://nas" into host "ftp".
        var candidate = trimmed.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal)
            ? trimmed
            : string.Concat(Uri.UriSchemeHttp, Uri.SchemeDelimiter, trimmed);

        if (HttpUriUtils.TryCreate(candidate, out var uri))
        {
            // Relative resolution keeps a base address's path only up to its last slash.
            var baseAddress = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
            return Uri.TryCreate(
                baseAddress.EndsWith('/') ? baseAddress : baseAddress + '/',
                UriKind.Absolute,
                out address);
        }

        address = null;
        return false;
    }
}
