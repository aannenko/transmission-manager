using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TransmissionManager.Api.Common.Dto.AppInfo;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class ConnectionService(
    IWebAssemblyHostEnvironment hostEnvironment,
    IHttpClientFactory httpClientFactory,
    ServerTimeZoneService serverTimeZoneService)
#pragma warning restore CA1812
{
    public Uri BaseAddress { get; private set; } = new UriBuilder(hostEnvironment.BaseAddress) { Port = 9092 }.Uri;

    public async Task<GetAppInfoResponse> ConnectAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(nameof(TransmissionManagerClient));
        httpClient.BaseAddress = baseAddress;
        httpClient.Timeout = TimeSpan.FromSeconds(1);
        var apiClient = new TransmissionManagerClient(httpClient);

        var appInfo = await apiClient.GetAppInfoAsync(cancellationToken).ConfigureAwait(false);

        BaseAddress = baseAddress;
        serverTimeZoneService.Offset = appInfo.LocalTime.Offset;

        return appInfo;
    }
}
