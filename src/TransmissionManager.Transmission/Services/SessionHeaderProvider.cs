using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Transmission.Services;

/// <summary>Stores the current Transmission session header.</summary>
/// <param name="options">The session-header options.</param>
public sealed class SessionHeaderProvider(IOptionsMonitor<SessionHeaderProviderOptions> options)
{
    private volatile string _sessionHeaderValue = string.Empty;

    /// <summary>Gets the configured session-header name.</summary>
    /// <returns>The header name.</returns>
    public string SessionHeaderName => options.CurrentValue.SessionHeaderName;

    /// <summary>Gets or sets the current session-header value.</summary>
    /// <returns>The latest value supplied by Transmission.</returns>
    public string SessionHeaderValue
    {
        get => _sessionHeaderValue;
        set => _sessionHeaderValue = value;
    }
}
