using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

internal readonly record struct FindTorrentPageParameters(
    [property: Range(1, 1000)] int Take = 20,
    long AfterId = 0,
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code - tested after trimming
    [property: MinLength(1)] string? PropertyStartsWith = null,
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    bool? CronExists = null);
