using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

/// <summary>
/// Specifies that a value must be a regular expression that searches for a magnet link.
/// </summary>
/// <remarks>
/// Requires the pattern to contain <c>magnet:\?</c>, so that it cannot match arbitrary text.
/// Matching this shape does not prove the pattern compiles - <c>magnet:\?xt=(</c> passes and then
/// throws when built - so a value accepted here can still be rejected where the regex is used.
/// <see langword="null"/> and the empty string are valid; use <c>[Required]</c> to enforce
/// presence.
/// </remarks>
public sealed class MagnetRegexAttribute : RegularExpressionAttribute
{
    public MagnetRegexAttribute() : base(@"^.*magnet:\\\?.+$")
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid regex for magnet link search.";
    }
}
