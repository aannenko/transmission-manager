using Microsoft.Extensions.Logging;

namespace TransmissionManager.Api.Tests.Helpers;

internal sealed record RecordedLog(LogLevel Level, EventId EventId, string Message, Exception? Exception);

/// <summary>
/// Keeps every entry written through it, already formatted, so a test can assert on what was logged.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<RecordedLog> _records = [];

    public IReadOnlyList<RecordedLog> Records => _records;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _records.Add(new(logLevel, eventId, formatter(state, exception), exception));
    }

    IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
