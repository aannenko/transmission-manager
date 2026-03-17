using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class ConnectionService(
    IWebAssemblyHostEnvironment hostEnvironment,
    IHttpClientFactory httpClientFactory,
    LocalStorageService localStorage)
#pragma warning restore CA1812
{
    private const string _storageKey = "baseAddress";

    public Uri BaseAddress { get; private set; } = new UriBuilder(hostEnvironment.BaseAddress) { Port = 9092 }.Uri;

    public async Task LoadAsync()
    {
        var value = await localStorage.GetItemAsync(_storageKey).ConfigureAwait(false);
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            BaseAddress = uri;
    }

    public async Task<Version> ConnectAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(nameof(TransmissionManagerClient));
        httpClient.BaseAddress = baseAddress;
        httpClient.Timeout = TimeSpan.FromSeconds(1);
        var apiClient = new TransmissionManagerClient(httpClient);

        var version = await apiClient.GetAppVersionAsync(cancellationToken).ConfigureAwait(false);

        BaseAddress = baseAddress;
        await localStorage.SetItemAsync(_storageKey, baseAddress.AbsoluteUri).ConfigureAwait(false);

        return version;
    }
}
