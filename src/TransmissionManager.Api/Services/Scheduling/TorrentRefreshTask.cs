using Coravel.Invocable;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Services.Logging;

namespace TransmissionManager.Api.Services.Scheduling;

#pragma warning disable CA1812 // Uninstantiated class - this class gets instantiated by Coravel at run time
internal sealed partial class TorrentRefreshTask(
    Log<TorrentRefreshTask> log,
    IRefreshTorrentByIdHandler refreshHandler,
    long torrentId) : IInvocable, ICancellableInvocable
#pragma warning restore CA1812 // Uninstantiated class
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        log.ScheduledRefreshStarted(torrentId);

        var (result, _, transmissionResult, warning, errors, _) = await refreshHandler
            .RefreshTorrentByIdAsync(torrentId, CancellationToken)
            .ConfigureAwait(false);

        if (result is RefreshTorrentByIdResult.Refreshed)
            log.ScheduledRefreshSucceeded(torrentId, transmissionResult, warning);
        else
            log.ScheduledRefreshFailed(torrentId, ToLogText(errors), transmissionResult);
    }

    /// <remarks>
    /// A scheduled refresh has no caller to answer, so what a request would return keyed by the
    /// setting at fault is flattened into one line, keys included - they say what to go and fix.
    /// </remarks>
    private static string ToLogText(KeyValuePair<string, string[]>[] errors) =>
        string.Join("; ", errors.Select(static error => $"{error.Key}: {string.Join(", ", error.Value)}"));
}
