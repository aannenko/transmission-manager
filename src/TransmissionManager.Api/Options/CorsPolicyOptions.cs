using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Api.Options;

internal sealed class CorsPolicyOptions
{
    [Required]
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    public string[] Origins { get; set; } = [];

    [Required]
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    public string[] Headers { get; set; } = [];

    [Required]
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    public string[] Methods { get; set; } = [];

    [Required]
    public bool AllowCredentials { get; set; } = false;
}
