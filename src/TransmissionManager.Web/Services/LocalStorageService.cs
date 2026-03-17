using Microsoft.JSInterop;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class LocalStorageService(IJSRuntime jsRuntime)
#pragma warning restore CA1812
{
    public ValueTask<string?> GetItemAsync(string key) =>
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetItemAsync(string key, string value) =>
        jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask RemoveItemAsync(string key) =>
        jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}
