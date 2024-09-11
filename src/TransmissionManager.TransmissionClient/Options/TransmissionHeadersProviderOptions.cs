using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.TransmissionClient.Options;

public sealed class TransmissionHeadersProviderOptions
{
    [Required]
    public required string SessionHeaderName { get; set; }
}
