using Microsoft.Extensions.Options;
using TransmissionManager.TransmissionClient.Options;

namespace TransmissionManager.TransmissionClient.Services;

public sealed class SessionHeaderProvider(IOptionsMonitor<TransmissionHeadersProviderOptions> options)
{
    public string SessionHeaderName => options.CurrentValue.SessionHeaderName;

    public string SessionHeaderValue { get; set; } = string.Empty;
}
