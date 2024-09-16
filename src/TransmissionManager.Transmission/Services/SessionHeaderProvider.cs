using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Transmission.Services;

public sealed class SessionHeaderProvider(IOptionsMonitor<SessionHeaderProviderOptions> options)
{
    public string SessionHeaderName => options.CurrentValue.SessionHeaderName;

    public string SessionHeaderValue { get; set; } = string.Empty;
}
