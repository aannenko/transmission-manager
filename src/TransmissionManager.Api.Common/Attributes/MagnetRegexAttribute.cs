using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

public sealed class MagnetRegexAttribute : RegularExpressionAttribute
{
    public MagnetRegexAttribute() : base(@"^.*magnet:\\\?.+$")
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid regex for magnet link search.";
    }
}
