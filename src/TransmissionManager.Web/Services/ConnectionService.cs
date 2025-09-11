using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class ConnectionService(
    IWebAssemblyHostEnvironment hostEnvironment,
    IHttpClientFactory httpClientFactory)
#pragma warning restore CA1812
{
    public Uri BaseAddress { get; private set; } = new UriBuilder(hostEnvironment.BaseAddress) { Port = 9092 }.Uri;

    public async Task<Version> ConnectAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(nameof(TransmissionManagerClient));
        httpClient.BaseAddress = baseAddress;
        httpClient.Timeout = TimeSpan.FromSeconds(1);
        var apiClient = new TransmissionManagerClient(httpClient);

        var version = await apiClient.GetAppVersionAsync(cancellationToken).ConfigureAwait(false);

        BaseAddress = baseAddress;

        return version;
    }
}
