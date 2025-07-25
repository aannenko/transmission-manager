﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed class UpdateTorrentByIdRequest
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [MinLength(1)]
    public string? DownloadDir { get; init; }

    [RegularExpression(RegexPatterns.IsFindMagnet, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? MagnetRegexPattern { get; init; }

    [RegularExpression(RegexPatterns.IsCron, MatchTimeoutInMilliseconds = 50)] // empty strings are valid
    public string? Cron { get; init; }
}
