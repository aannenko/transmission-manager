using System.Collections.Concurrent;
using TransmissionManager.Api.Composite.Dto;
using TransmissionManager.Api.Composite.Extensions;
using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Models;
using TransmissionManager.Api.Transmission.Services;
using static TransmissionManager.Api.Composite.Dto.AddOrUpdateTorrentResult;

namespace TransmissionManager.Api.Composite.Services;

public sealed class CompositeService<TTorrentService>(
    MagnetUriRetriever magnetRetriever,
    TransmissionClient transmissionClient,
    TTorrentService torrentService)
    where TTorrentService : ITorrentService
{
    private static readonly TransmissionTorrentGetRequestFields[] _getNameOnlyFieldsArray =
        [TransmissionTorrentGetRequestFields.Name];

    private static readonly ConcurrentDictionary<long, bool> _runningNameUpdates = [];

    public async Task<AddOrUpdateTorrentResult> AddOrUpdateTorrentAsync(
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        var (magnetUri, trackerError) =
            await GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken);
        
        if (string.IsNullOrEmpty(magnetUri))
            return new(ResultType.Error, -1, trackerError);

        var (transmissionTorrent, transmissionError) =
            await SendMagnetToTransmissionAsync(magnetUri, dto.DownloadDir, cancellationToken);
        
        if (transmissionTorrent is null)
            return new(ResultType.Error, -1, transmissionError);

        var torrentId = torrentService.FindPage(new(1, 0, WebPageUri: dto.WebPageUri)).SingleOrDefault()?.Id;
        var resultType = torrentId is null ? ResultType.Add : ResultType.Update;
        TorrentUpdateDto? updateDto = null;
        if (torrentId is null)
            torrentId = torrentService.AddOne(dto.ToTorrentAddDto(transmissionTorrent));
        else
            torrentService.TryUpdateOneById(torrentId.Value, updateDto = dto.ToTorrentUpdateDto(transmissionTorrent));

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = UpdateTorrentNameWithRetriesAsync(
                torrentId.Value,
                updateDto ?? dto.ToTorrentUpdateDto(transmissionTorrent));

        return new(resultType, torrentId.Value, null);
    }

    public async Task<string?> RefreshTorrentAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        const string error = "Refresh of the torrent with id '{0}' has failed: '{1}'.";
        var torrent = torrentService.FindOneById(torrentId);
        if (torrent is null)
            return string.Format(error, torrentId, "No such torrent.");

        var (transmissionGetTorrent, transmissionGetError) =
            await GetTorrentFromTransmissionAsync(torrent.TransmissionId, cancellationToken);

        if (transmissionGetTorrent is null)
            return string.Format(error, torrentId, transmissionGetError);

        var (magnetUri, trackerError) =
           await GetMagnetUriAsync(torrent.WebPageUri, torrent.MagnetRegexPattern, cancellationToken);

        if (string.IsNullOrEmpty(magnetUri))
            return string.Format(error, torrentId, trackerError);

        var (transmissionAddTorrent, transmissionAddError) =
            await SendMagnetToTransmissionAsync(magnetUri, torrent.DownloadDir, cancellationToken);

        if (transmissionAddTorrent is null)
            return string.Format(error, torrentId, transmissionAddError);

        var updateDto = torrent.ToTorrentUpdateDto(transmissionAddTorrent);
        if (!torrentService.TryUpdateOneById(torrent.Id, updateDto))
            return string.Format(error, torrentId, "No such torrent.");

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = UpdateTorrentNameWithRetriesAsync(torrentId, updateDto);

        return null;
    }

    private async Task<(string? Magnet, string? Error)> GetMagnetUriAsync(
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

    private async Task<(TransmissionTorrentAddResponseItem? Torrent, string? Error)> SendMagnetToTransmissionAsync(
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

    private async Task<(TransmissionTorrentGetResponseItem? Torrent, string? Error)> GetTorrentFromTransmissionAsync(
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
            error = $"Could not get a torrent with a Transmission id '{transmissionId}' from Transmission{error}.";

        return new(transmissionTorrent, error);
    }

    private async Task UpdateTorrentNameWithRetriesAsync(long id, TorrentUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto?.TransmissionId);

        if (!_runningNameUpdates.TryAdd(id, true))
            return;

        long[] singleTransmissionIdArray = [dto.TransmissionId.Value];
        
        const int numberOfRetries = 40; // make attempts to get the name for 6 hours
        for (int i = 1, millisecondsDelay = i * i * 1000; i <= numberOfRetries; i++)
        {
            await Task.Delay(millisecondsDelay).ConfigureAwait(false);

            TransmissionTorrentGetResponse? transmissionResponse = null;
            try
            {
                transmissionResponse = await transmissionClient
                    .GetTorrentsAsync(singleTransmissionIdArray, _getNameOnlyFieldsArray)
                    .ConfigureAwait(false);
            }
            catch
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

        _runningNameUpdates.TryRemove(id, out _);
    }
}
