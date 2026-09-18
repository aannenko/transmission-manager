using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TransmissionManager.Web.Dto;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class ApiAddressService(
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
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            BaseAddress = uri;
        }
    }

    public async Task<ApiResult<Version>> ConnectAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(nameof(TransmissionManagerClient));
        httpClient.BaseAddress = baseAddress;
        httpClient.Timeout = TimeSpan.FromSeconds(1);
        var apiClient = new TransmissionManagerClient(httpClient);

        var result = await apiClient.GetAppVersionAsync(cancellationToken).ConfigureAwait(false);
        if (result.Status is not ApiResultStatus.Success)
            return result;

        BaseAddress = baseAddress;
        await localStorage.SetItemAsync(_storageKey, baseAddress.AbsoluteUri).ConfigureAwait(false);

        return result;
    }
}
