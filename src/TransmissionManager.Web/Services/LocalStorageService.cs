using Microsoft.JSInterop;

namespace TransmissionManager.Web.Services;

// A browser set to block site data throws on every call, arriving here as JSException. The block is
// a setting the user or an administrator chose, not a fault.
#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class LocalStorageService(IJSRuntime jsRuntime)
#pragma warning restore CA1812
{
    public async ValueTask<string?> GetItemAsync(string key)
    {
        try
        {
            return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key).ConfigureAwait(false);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async ValueTask<bool> TrySetItemAsync(string key, string value)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value).ConfigureAwait(false);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }
}
