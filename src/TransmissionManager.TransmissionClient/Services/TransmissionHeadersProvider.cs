using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Transmission.Services;

public sealed class TransmissionHeadersProvider(IOptionsMonitor<TransmissionHeadersProviderOptions> options)
{
    public string SessionHeaderName => options.CurrentValue.SessionHeaderName;

    public string SessionHeaderValue { get; set; } = string.Empty;
}
