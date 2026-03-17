using TransmissionManager.Web.Constants;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class ThemeService(LocalStorageService localStorage)
#pragma warning restore CA1812
{
    private const string _storageKey = "theme";

    public Theme Theme { get; private set; } = Theme.Light;

    public async Task LoadAsync()
    {
        var value = await localStorage.GetItemAsync(_storageKey).ConfigureAwait(false);
        Theme = Enum.TryParse<Theme>(value, ignoreCase: true, out var theme) ? theme : Theme.Light;
    }

    public async Task SetThemeAsync(Theme theme)
    {
        Theme = theme;
        await localStorage.SetItemAsync(_storageKey, theme.ToString().ToLowerInvariant()).ConfigureAwait(false);
    }
}
