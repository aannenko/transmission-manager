using System.Collections.Concurrent;
using TransmissionManager.Api.Composite.Extensions;
using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Models;
using TransmissionManager.Api.Transmission.Services;

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

    public async Task<bool> TryAddOrUpdateTorrentAsync(
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        var transmissionTorrent = await GetTransmissionTorrentAsync(
            dto.WebPageUri,
            dto.MagnetRegexPattern,
            dto.DownloadDir,
            cancellationToken);

        if (transmissionTorrent is null)
            return false;

        var torrentId = torrentService.FindPage(new(1, 0, WebPageUri: dto.WebPageUri)).SingleOrDefault()?.Id;
        TorrentUpdateDto? updateDto = null;
        if (torrentId is null)
            torrentId = torrentService.AddOne(dto.ToTorrentAddDto(transmissionTorrent));
        else
            torrentService.TryUpdateOneById(torrentId.Value, updateDto = dto.ToTorrentUpdateDto(transmissionTorrent));

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = UpdateTorrentNameWithRetriesAsync(
                torrentId.Value,
                updateDto ?? dto.ToTorrentUpdateDto(transmissionTorrent));

        return true;
    }

    public async Task<bool> TryRefreshTorrentAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        var torrent = torrentService.FindOneById(torrentId);

        if (torrent is null)
            return false;

        var transmissionGetResponse = await transmissionClient
            .GetTorrentsAsync([torrent.TransmissionId], _getNameOnlyFieldsArray, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetResponse.Arguments?.Torrents?.Length is null or 0)
            return false;

        var transmissionTorrent = await GetTransmissionTorrentAsync(
            torrent.WebPageUri,
            torrent.MagnetRegexPattern,
            torrent.DownloadDir,
            cancellationToken);

        if (transmissionTorrent is null)
            return false;

        var updateDto = torrent.ToTorrentUpdateDto(transmissionTorrent);
        if (!torrentService.TryUpdateOneById(torrent.Id, updateDto))
            return false;

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = UpdateTorrentNameWithRetriesAsync(torrentId, updateDto);

        return true;
    }

    private async ValueTask<TransmissionTorrentAddResponseItem?> GetTransmissionTorrentAsync(
        string webPageUri,
        string? magnetRegexPattern,
        string downloadDir,
        CancellationToken cancellationToken)
    {
        var magnetUri = await magnetRetriever
            .FindMagnetUri(webPageUri, magnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return null;

        var transmissionResponse = await transmissionClient
            .AddTorrentMagnetAsync(magnetUri, downloadDir, cancellationToken)
            .ConfigureAwait(false);

        return transmissionResponse.Arguments?.TorrentAdded ?? transmissionResponse.Arguments?.TorrentDuplicate;
    }

    private async Task UpdateTorrentNameWithRetriesAsync(long id, TorrentUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto?.TransmissionId);

        if (!_runningNameUpdates.TryAdd(dto.TransmissionId.Value, true))
            return;

        long[] singleTransmissionIdArray = [dto.TransmissionId.Value];
        
        const int numberOfRetries = 40; // make attempts to get the name for 6 hours
        for (int i = 1, delaySeconds = i * i; i <= numberOfRetries; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds)).ConfigureAwait(false);

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

        _runningNameUpdates.TryRemove(dto.TransmissionId.Value, out _);
    }
}
