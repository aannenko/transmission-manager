using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

/// <summary>
/// Specifies that a value must be a format that builds a magnet link out of an extracted value,
/// which <c>{0}</c> stands for.
/// </summary>
/// <remarks>
/// The value is substituted by composite formatting, which honours far more than a placeholder, so
/// requiring nothing but literal text and <c>{0}</c> is what keeps a format from asking for a second
/// argument (<c>{1}</c>, which throws where the magnet is built) or an alignment
/// (<c>{0,1000000}</c>, a megabyte built from a forty-character hash). A bare <c>{0}</c> is valid,
/// and is what a source already holding whole magnet links needs.
/// <see langword="null"/> and the empty string are valid and defer to the configured default.
/// <para>
/// Mirrors <c>JsonValueRegex.IsJsonValueFormat</c> in the torrent sources project, which this
/// project cannot reference; a test asserts the two stay identical.
/// </para>
/// </remarks>
public sealed class JsonValueFormatAttribute : RegularExpressionAttribute
{
    public JsonValueFormatAttribute() : base(@"^[^{}]*(\{0\}[^{}]*)+$")
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid magnet format - it must contain '{{0}}' and no other braces.";
    }
}
