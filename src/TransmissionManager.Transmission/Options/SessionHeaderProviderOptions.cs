using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Transmission.Options;

/// <summary>Configures Transmission's session header.</summary>
public sealed class SessionHeaderProviderOptions
{
    /// <summary>Gets or sets the session-header name.</summary>
    /// <returns>The header name.</returns>
    [Required]
    public required string SessionHeaderName { get; set; }
}
