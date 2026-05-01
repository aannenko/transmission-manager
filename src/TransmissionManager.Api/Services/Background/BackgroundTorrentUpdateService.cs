using System.Collections.Concurrent;
using TransmissionManager.Api.Services.Logging;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Services.Background;

internal sealed class BackgroundTorrentUpdateService(
    IServiceScopeFactory serviceScopeFactory,
    Log<BackgroundTorrentUpdateService> log,
    TimeProvider timeProvider)
{
    private static readonly TransmissionTorrentGetRequestFields[] _getNameOnlyFieldsArray =
        [TransmissionTorrentGetRequestFields.Name];

    private readonly ConcurrentDictionary<long, CancellationTokenSource> _runningNameUpdates = [];

    public async Task UpdateTorrentNameAsync(long id, string hashString, string currentName, long version)
    {
        using var cts = _runningNameUpdates.AddOrUpdate(id, AddCts, UpdateCts);
#pragma warning disable CA1031 // Do not catch general exception types - method is used as fire-and-forget, log errors
        try
        {
            await UpdateTorrentNameWithRetriesAsync(id, hashString, currentName, version, cts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            log.BackgroundNameUpdateFailed(id, e);
        }
        finally
        {
            _ = _runningNameUpdates.TryRemove(KeyValuePair.Create(id, cts));
        }
#pragma warning restore CA1031 // Do not catch general exception types

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
    }

    private async Task UpdateTorrentNameWithRetriesAsync(
        long id,
        string hashString,
        string currentName,
        long version,
        CancellationToken cancellationToken)
    {
        string[] singleHashArray = [hashString];

        using var serviceScope = serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var transmissionClient = serviceProvider.GetRequiredService<TransmissionClient>();

        const int maxRetries = 40; // make attempts to get the name for approximately 6 hours
        for (var retry = 1; retry <= maxRetries; retry++)
        {
            await Task.Delay(TimeSpan.FromSeconds(retry * retry), timeProvider, cancellationToken).ConfigureAwait(false);

            TransmissionTorrentGetResponse? transmissionResponse = null;
            try
            {
                transmissionResponse = await transmissionClient
                    .GetTorrentsAsync(singleHashArray, _getNameOnlyFieldsArray, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException) when (retry < maxRetries)
            {
                continue;
            }

            var newName = transmissionResponse?.Arguments?.Torrents?.SingleOrDefault()?.Name;
            if (newName != hashString)
            {
                if (string.IsNullOrWhiteSpace(newName) || newName == currentName)
                    break;

                var torrentService = serviceProvider.GetRequiredService<TorrentService>();
                var dto = new TorrentUpdateDto(name: newName);

                const int maxConcurrencyRetries = 3;
                for (var concurrencyRetry = 0; concurrencyRetry < maxConcurrencyRetries; concurrencyRetry++)
                {
                    var outcome = await torrentService
                        .UpdateOneAsync(id, version, dto, cancellationToken)
                        .ConfigureAwait(false);

                    if (outcome.Result is TorrentMutationResult.Success or TorrentMutationResult.NotFound)
                        return;

                    var current = await torrentService
                        .FindOneByIdAsync(id, cancellationToken)
                        .ConfigureAwait(false);

                    if (current is null || current.HashString != hashString || current.Name != currentName)
                        return;

                    version = current.Version;
                }

                log.BackgroundUpdateSkippedDueToConcurrency(id);
                return;
            }
        }
    }
}
