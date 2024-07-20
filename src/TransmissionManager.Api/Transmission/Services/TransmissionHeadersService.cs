using Microsoft.Extensions.Options;
using TransmissionManager.Api.Transmission.Options;

namespace TransmissionManager.Api.Transmission.Services;

public sealed class TransmissionHeadersService(IOptionsMonitor<TransmissionHeadersServiceOptions> options)
{
    public string SessionHeaderName => options.CurrentValue.SessionHeaderName;

    public string SessionHeaderValue { get; set; } = string.Empty;
}
