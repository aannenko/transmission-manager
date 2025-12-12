using System.Collections.Concurrent;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Services.Background;

internal sealed class BackgroundTorrentUpdateService(IServiceScopeFactory serviceScopeFactory)
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
                oldCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }

            return new();
        }

        using var cts = _runningNameUpdates.AddOrUpdate(id, AddCts, UpdateCts);
        try
        {
            using var serviceScope = serviceScopeFactory.CreateScope();
            await UpdateTorrentNameWithRetriesAsync(serviceScope.ServiceProvider, id, hashString, cts.Token)
                .ConfigureAwait(false);
        }
        finally
        {
            _ = _runningNameUpdates.TryRemove(id, out _);
        }
    }

    private static async Task UpdateTorrentNameWithRetriesAsync(
        IServiceProvider serviceProvider,
        long id,
        string hashString,
        CancellationToken cancellationToken)
    {
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

                var torrentService = serviceProvider.GetRequiredService<TorrentService>();
                var dto = new TorrentUpdateDto(name: torrentName);
                _ = await torrentService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
                break;
            }
        }
    }
}
