using Microsoft.Extensions.Options;

namespace TransmissionManager.BaseTests.Options;

public sealed class MockOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
{
    public TOptions CurrentValue => value;

    public TOptions Get(string? name) => value;

    public IDisposable? OnChange(Action<TOptions, string?> listener) =>
        throw new NotImplementedException();
}
