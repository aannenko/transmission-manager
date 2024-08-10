using System.Collections.Concurrent;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Models;
using TransmissionManager.Api.Transmission.Services;

namespace TransmissionManager.Api.Composite.Services;

public abstract class BaseCompositeTorrentService(
    MagnetUriRetriever magnetRetriever,
    TransmissionClient transmissionClient,
    BackgroundTaskService backgroundTaskService)
{
    private static readonly TransmissionTorrentGetRequestFields[] _getNameOnlyFieldsArray =
        [TransmissionTorrentGetRequestFields.Name];

    private static readonly ConcurrentDictionary<long, CancellationTokenSource> _runningNameUpdates = [];

    protected async Task<(string? Magnet, string? Error)> GetMagnetUriAsync(
        string webPageUri,
        string? magnetRegexPattern,
        CancellationToken cancellationToken)
    {
        string? magnetUri = null;
        string error = string.Empty;
        try
        {
            magnetUri = await magnetRetriever
                .FindMagnetUriAsync(webPageUri, magnetRegexPattern, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException or ArgumentException or InvalidOperationException)
        {
            error = $": '{e.Message}'";
        }

        return magnetUri is null
            ? (null, $"Could not retrieve a magnet link from '{webPageUri}'{error}.")
            : (magnetUri, null);
    }

    protected async Task<(TransmissionTorrentAddResponseItem? Torrent, string? Error)> SendMagnetToTransmissionAsync(
        string magnetUri,
        string downloadDir,
        CancellationToken cancellationToken)
    {
        TransmissionTorrentAddResponse? transmissionResponse = null;
        string error = string.Empty;
        try
        {
            transmissionResponse = await transmissionClient
                .AddTorrentUsingMagnetUriAsync(magnetUri, downloadDir, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            error = $": '{e.Message}'";
        }

        var transmissionTorrent =
            transmissionResponse?.Arguments?.TorrentAdded ?? transmissionResponse?.Arguments?.TorrentDuplicate;

        if (transmissionTorrent is null)
            error = $"Could not add a torrent to Transmission{error}.";

        return (transmissionTorrent, error);
    }

    protected async Task<(TransmissionTorrentGetResponseItem? Torrent, string? Error)> GetTorrentFromTransmissionAsync(
        long transmissionId,
        CancellationToken cancellationToken)
    {
        TransmissionTorrentGetResponse? transmissionResponse = null;
        string error = string.Empty;
        try
        {
            transmissionResponse = await transmissionClient
                .GetTorrentsAsync([transmissionId], cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            error = $": '{e.Message}'";
        }

        TransmissionTorrentGetResponseItem? transmissionTorrent = null;
        if (transmissionResponse?.Arguments?.Torrents?.Length is 1)
            transmissionTorrent = transmissionResponse.Arguments.Torrents[0];
        else
            error = $"Could not get a torrent with the Transmission id '{transmissionId}' from Transmission{error}.";

        return new(transmissionTorrent, error);
    }

    protected async Task StartUpdateTorrentNameTask(long id, TorrentUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto?.TransmissionId);

        if (_runningNameUpdates.TryRemove(id, out var oldCts))
            oldCts.Cancel();

        using var newCts = new CancellationTokenSource();
        if (!_runningNameUpdates.TryAdd(id, newCts))
            return;

        try
        {
            await backgroundTaskService.RunScopedAsync(UpdateTorrentNameWithRetriesAsync, (id, dto), newCts.Token);
        }
        finally
        {
            _runningNameUpdates.TryRemove(id, out _);
        }
    }

    private static async Task UpdateTorrentNameWithRetriesAsync(
        IServiceProvider serviceProvider,
        (long, TorrentUpdateDto) argument,
        CancellationToken cancellationToken)
    {
        var (id, dto) = argument;

        var transmissionClient = serviceProvider.GetRequiredService<TransmissionClient>();
        var torrentService = serviceProvider.GetRequiredService<TorrentService>();

        long[] singleTransmissionIdArray = [dto.TransmissionId!.Value];

        const int numberOfRetries = 40; // make attempts to get the name for 6 hours
        for (int i = 1, millisecondsDelay = i * i * 1000; i <= numberOfRetries; i++)
        {
            await Task.Delay(millisecondsDelay, cancellationToken).ConfigureAwait(false);

            TransmissionTorrentGetResponse? transmissionResponse = null;
            try
            {
                transmissionResponse = await transmissionClient
                    .GetTorrentsAsync(singleTransmissionIdArray, _getNameOnlyFieldsArray, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                if (i is numberOfRetries)
                    throw;
            }

            var newName = transmissionResponse?.Arguments?.Torrents?.SingleOrDefault()?.Name;
            if (string.IsNullOrEmpty(newName))
            {
                break;
            }
            else if (dto.Name != newName)
            {
                dto.Name = newName;
                torrentService.TryUpdateOneById(id, dto);
                break;
            }
        }
    }
}
