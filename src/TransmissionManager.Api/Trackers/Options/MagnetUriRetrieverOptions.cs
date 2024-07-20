using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Utilities;

namespace TransmissionManager.Api.Trackers.Options;

public sealed class MagnetUriRetrieverOptions
{
    [Required]
    [RegularExpression(AppRegex.FindMagnet)]
    public required string DefaultRegexPattern { get; set; }

    [Required]
    [Range(50, 1000)]
    public required int RegexMatchTimeoutMilliseconds { get; set; }
}
