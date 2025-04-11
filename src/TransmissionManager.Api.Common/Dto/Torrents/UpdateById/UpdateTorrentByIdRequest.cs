using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Common.Dto.Torrents.UpdateById;

public sealed class UpdateTorrentByIdRequest
{
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code - tested after trimming
    [MinLength(1)]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    public string? DownloadDir { get; init; }

    [RegularExpression(RegexPatterns.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? MagnetRegexPattern { get; init; }

    [RegularExpression(RegexPatterns.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? Cron { get; init; }
}
