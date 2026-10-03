using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TransmissionManager.Api.Common.Validation;
using TransmissionManager.Web.Dto;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class ApiAddressService(
    IWebAssemblyHostEnvironment hostEnvironment,
    IHttpClientFactory httpClientFactory,
    LocalStorageService localStorage)
#pragma warning restore CA1812
{
    internal sealed record ConnectionOutcome(Version Version, bool IsAddressSaved);

    private const string _storageKey = "baseAddress";

    public Uri BaseAddress { get; private set; } =
        new UriBuilder(hostEnvironment.BaseAddress) { Port = 9092, Path = "/" }.Uri;

    public async Task LoadAsync()
    {
        var value = await localStorage.GetItemAsync(_storageKey).ConfigureAwait(false);
        if (HttpUriUtils.TryCreate(value, out var uri))
            BaseAddress = uri;
    }

    public async Task<ApiResult<ConnectionOutcome>> ConnectAsync(
        Uri baseAddress,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(nameof(TransmissionManagerClient));
        httpClient.BaseAddress = baseAddress;
        httpClient.Timeout = TimeSpan.FromSeconds(1);
        var apiClient = new TransmissionManagerClient(httpClient);

        var result = await apiClient.GetAppVersionAsync(cancellationToken).ConfigureAwait(false);
        if (result is not { Status: ApiResultStatus.Success, StatusCode: { } statusCode, Value: { } version })
            return ApiResult<ConnectionOutcome>.Failure(result.StatusCode, result.ProblemDetails);

        BaseAddress = baseAddress;
        var isSaved = await localStorage.TrySetItemAsync(_storageKey, baseAddress.AbsoluteUri).ConfigureAwait(false);

        return ApiResult<ConnectionOutcome>.Success(statusCode, new(version, isSaved));
    }
}
