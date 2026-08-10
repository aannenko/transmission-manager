using Microsoft.Extensions.Logging;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Logging;
using TransmissionManager.Api.Services.Scheduling;

namespace TransmissionManager.Api.Tests.Services.Scheduling;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentRefreshTaskTests
{
    private const long _torrentId = 42;

    // A Duplicate refresh is a no-change success (RefreshTorrentByIdResult.Refreshed). It must be
    // logged as succeeded - guards against discriminating on the TransmissionAddResult (which would
    // treat Duplicate as a failure).
    [Test]
    public async Task Invoke_WhenRefreshSucceedsAsDuplicate_LogsScheduledRefreshSucceeded()
    {
        var outcome = new RefreshTorrentByIdOutcome(
            RefreshTorrentByIdResult.Refreshed, null, TransmissionAddResult.Duplicate, Message: null);

        var logs = await RunAsync(outcome).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logs, Has.Some.Matches<RecordedLog>(IsSucceeded));
            Assert.That(logs, Has.None.Matches<RecordedLog>(static log => log.Level is LogLevel.Warning));
        }
    }

    // An Added refresh whose old-torrent cleanup failed is still a success, but carries a message.
    // It must be logged as succeeded - guards against discriminating on the presence of a message
    // (which would treat this success as a failure).
    [Test]
    public async Task Invoke_WhenRefreshSucceedsWithCleanupWarning_LogsScheduledRefreshSucceeded()
    {
        var outcome = new RefreshTorrentByIdOutcome(
            RefreshTorrentByIdResult.Refreshed, null, TransmissionAddResult.Added, Message: "old torrent cleanup failed");

        var logs = await RunAsync(outcome).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logs, Has.Some.Matches<RecordedLog>(IsSucceeded));
            Assert.That(logs, Has.None.Matches<RecordedLog>(static log => log.Level is LogLevel.Warning));
        }
    }

    [TestCase(RefreshTorrentByIdResult.NotFoundLocally)]
    [TestCase(RefreshTorrentByIdResult.NotFoundInTransmission)]
    [TestCase(RefreshTorrentByIdResult.Removed)]
    [TestCase(RefreshTorrentByIdResult.VersionConflict)]
    [TestCase(RefreshTorrentByIdResult.Exists)]
    [TestCase(RefreshTorrentByIdResult.InvalidConfiguration)]
    [TestCase(RefreshTorrentByIdResult.DependencyFailed)]
    public async Task Invoke_WhenRefreshDoesNotSucceed_LogsScheduledRefreshFailed(RefreshTorrentByIdResult result)
    {
        var outcome = new RefreshTorrentByIdOutcome(result, null, null, Message: "refresh failed");

        var logs = await RunAsync(outcome).ConfigureAwait(false);

        Assert.That(
            logs,
            Has.Some.Matches<RecordedLog>(static log =>
                log.Level is LogLevel.Warning && log.Message.Contains("failed", StringComparison.Ordinal)));
    }

    private static async Task<IReadOnlyList<RecordedLog>> RunAsync(RefreshTorrentByIdOutcome outcome)
    {
        var logger = new RecordingLogger<TorrentRefreshTask>();
        var task = new TorrentRefreshTask(
            new Log<TorrentRefreshTask>(logger),
            new FakeRefreshTorrentByIdHandler(outcome),
            _torrentId);

        await task.Invoke().ConfigureAwait(false);

        return logger.Records;
    }

    private static bool IsSucceeded(RecordedLog log) =>
        log.Level is LogLevel.Information && log.Message.Contains("succeeded", StringComparison.Ordinal);

    private sealed class FakeRefreshTorrentByIdHandler(RefreshTorrentByIdOutcome outcome) : IRefreshTorrentByIdHandler
    {
        public Task<RefreshTorrentByIdOutcome> RefreshTorrentByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(outcome);
    }

    private sealed record RecordedLog(LogLevel Level, string Message, Exception? Exception);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<RecordedLog> _records = [];

        public IReadOnlyList<RecordedLog> Records => _records;

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            _records.Add(new(logLevel, formatter(state, exception), exception));
        }
    }
}
