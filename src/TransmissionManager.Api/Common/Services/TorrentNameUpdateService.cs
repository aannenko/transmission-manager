using System.Collections.Concurrent;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class TorrentNameUpdateService(BackgroundTaskService backgroundTaskService)
{
    private static readonly TransmissionTorrentGetRequestFields[] _getNameOnlyFieldsArray =
        [TransmissionTorrentGetRequestFields.Name];

    private readonly ConcurrentDictionary<long, CancellationTokenSource> _runningNameUpdates = [];

    public async Task UpdateTorrentNameAsync(long id, string hashString)
    {
        static CancellationTokenSource AddCts(long _) => new();

        static CancellationTokenSource UpdateCts(long _, CancellationTokenSource oldCts)
        {
            try
            {
                oldCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            return new();
        }

        using var cts = _runningNameUpdates.AddOrUpdate(id, AddCts, UpdateCts);

        try
        {
            await backgroundTaskService
                .RunScopedAsync(UpdateTorrentNameWithRetriesAsync, (id, hashString), cts.Token)
                .ConfigureAwait(false);
        }
        finally
        {
            _runningNameUpdates.TryRemove(id, out _);
        }
    }

    private static async Task UpdateTorrentNameWithRetriesAsync(
        IServiceProvider serviceProvider,
        (long, string) torrentIdAndHashString,
        CancellationToken cancellationToken)
    {
        var (id, hashString) = torrentIdAndHashString;
        string[] singleHashArray = [hashString];

        var transmissionClient = serviceProvider.GetRequiredService<TransmissionClient>();

        const int maxRetries = 40; // make attempts to get the name for approximately 6 hours
        for (var retry = 1; retry <= maxRetries; retry++)
        {
            await Task.Delay(TimeSpan.FromSeconds(retry * retry), cancellationToken).ConfigureAwait(false);

            TransmissionTorrentGetResponse? transmissionResponse = null;
            try
            {
                transmissionResponse = await transmissionClient
                    .GetTorrentsAsync(singleHashArray, _getNameOnlyFieldsArray, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException) when (retry < maxRetries)
            {
            }

            var torrentName = transmissionResponse?.Arguments?.Torrents?.SingleOrDefault()?.Name;
            if (torrentName != hashString)
            {
                if (string.IsNullOrEmpty(torrentName))
                    break;

                var torrentCommandService = serviceProvider.GetRequiredService<TorrentCommandService>();
                var dto = new TorrentUpdateDto(name: torrentName);
                await torrentCommandService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
                break;
            }
        }
    }
}
