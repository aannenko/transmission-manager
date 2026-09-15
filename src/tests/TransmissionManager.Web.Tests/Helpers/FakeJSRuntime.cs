using Microsoft.JSInterop;

namespace TransmissionManager.Web.Tests.Helpers;

/// <summary>
/// Stands in for the browser's <c>localStorage</c>, keeping whatever the calls write in
/// <see cref="Storage"/> so a test can seed it or read it back.
/// </summary>
internal sealed class FakeJSRuntime : IJSRuntime
{
    public Dictionary<string, string?> Storage { get; } = new(StringComparer.Ordinal);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        switch (identifier)
        {
            case "localStorage.getItem":
                _ = Storage.TryGetValue(Key(args), out var value);
                return ValueTask.FromResult((TValue)(object?)value!);

            case "localStorage.setItem":
                Storage[Key(args)] = args![1] as string;
                return default;

            case "localStorage.removeItem":
                _ = Storage.Remove(Key(args));
                return default;

            default:
                throw new NotSupportedException($"Unexpected JavaScript call '{identifier}'.");
        }
    }

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }

    private static string Key(object?[]? args)
    {
        return (string)args![0]!;
    }
}
