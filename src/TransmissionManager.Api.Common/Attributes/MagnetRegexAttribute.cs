using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

public sealed class MagnetRegexAttribute : RegularExpressionAttribute
{
    private const string _isMagnetRegex = @"^.*magnet:\\\?.+$";

    public MagnetRegexAttribute() : base(_isMagnetRegex)
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid regex for magnet link search.";
    }
}
