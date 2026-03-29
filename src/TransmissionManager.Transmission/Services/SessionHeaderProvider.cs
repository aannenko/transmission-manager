using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Transmission.Services;

public sealed class SessionHeaderProvider(IOptionsMonitor<SessionHeaderProviderOptions> options)
{
    private volatile string _sessionHeaderValue = string.Empty;

    public string SessionHeaderName => options.CurrentValue.SessionHeaderName;

    public string SessionHeaderValue
    {
        get => _sessionHeaderValue;
        set => _sessionHeaderValue = value;
    }
}
