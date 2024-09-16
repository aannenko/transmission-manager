using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Transmission.Options;

public sealed class SessionHeaderProviderOptions
{
    [Required]
    public required string SessionHeaderName { get; set; }
}
